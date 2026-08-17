using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Domain.Sync.ValueObjects;
using Amber.Infrastructure.Common;
using Amber.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Amber.Infrastructure.Sync.Repositories;

public class SyncCellRepository(AmberContext amberContext)
    : UnitOfWorkRepositoryBase(amberContext),
        ISyncCellRepository
{
    public async Task<bool> TryUpsertCellsAsync(
        Guid userId,
        IList<SyncCell> cells,
        long maxStoragePerUserInBytes,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = await AmberContext.Database.BeginTransactionAsync(
            cancellationToken
        );

        // Keep only the last-writing cell per (table, row, column) within the batch itself,
        // so a batch that touches the same cell more than once doesn't race against itself.
        var dedupedCells = cells
            .GroupBy(c => (c.Id.Table, c.Id.RowId, c.Id.Column))
            .Select(g =>
                g.Aggregate((latest, next) => next.Hlc.IsAfter(latest.Hlc) ? next : latest)
            )
            .ToList();

        var nextServerSeq =
            await AmberContext
                .SyncCells.Where(c => c.UserId == userId)
                .Select(c => (long?)c.ServerSeq)
                .MaxAsync(cancellationToken) ?? 0;

        var tables = dedupedCells.Select(c => c.Id.Table).Distinct().ToList();
        var rowIds = dedupedCells.Select(c => c.Id.RowId).Distinct().ToList();
        var columns = dedupedCells.Select(c => c.Id.Column).Distinct().ToList();

        var candidates = await AmberContext
            .SyncCells.Where(c =>
                c.UserId == userId
                && tables.Contains(c.Table)
                && rowIds.Contains(c.RowId)
                && columns.Contains(c.Column)
            )
            .ToListAsync(cancellationToken);

        var existingByKey = candidates.ToDictionary(c => new SyncCellId(
            userId,
            c.Table,
            c.RowId,
            c.Column
        ));

        foreach (var cell in dedupedCells)
        {
            existingByKey.TryGetValue(cell.Id, out var existing);

            if (existing is not null && !cell.Hlc.IsAfter(existing.Hlc))
            {
                continue;
            }

            nextServerSeq++;
            var writtenAt = DateTime.UtcNow;

            if (existing is null)
            {
                await AmberContext.SyncCells.AddAsync(
                    new SyncCell(
                        new SyncCellId(userId, cell.Id.Table, cell.Id.RowId, cell.Id.Column),
                        cell.Value,
                        cell.Hlc,
                        cell.DeviceId
                    )
                    {
                        ServerSeq = nextServerSeq,
                        WrittenAt = writtenAt,
                    }
                );
            }
            else
            {
                existing.Value = cell.Value;
                existing.Hlc = cell.Hlc;
                existing.DeviceId = cell.DeviceId;
                existing.ServerSeq = nextServerSeq;
                existing.WrittenAt = writtenAt;
            }
        }

        await AmberContext.SaveChangesAsync(cancellationToken);

        var usedStorage = await CalculateUserUsedStorageInBytesAsync(userId, cancellationToken);
        if (usedStorage > maxStoragePerUserInBytes)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IList<SyncCell>> GetCellsAfterServerSeqAsync(
        Guid userId,
        long sinceServerSeq,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        return await AmberContext
            .SyncCells.Where(c => c.UserId == userId && c.ServerSeq > sinceServerSeq)
            .OrderBy(c => c.ServerSeq)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> CalculateUserUsedStorageInBytesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await AmberContext
            .SyncCells.Where(c => c.UserId == userId)
            .SumAsync(c => c.SizeInBytes, cancellationToken);
    }
}
