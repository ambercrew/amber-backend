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
        // Keep only the last-writing cell per (table, row, column) within the batch itself,
        // so a batch that touches the same cell more than once doesn't race against itself.
        var dedupedCells = cells
            .GroupBy(c => (c.Id.Table, c.Id.RowId, c.Id.Column))
            .Select(g =>
                g.Aggregate((latest, next) => next.Hlc.IsAfter(latest.Hlc) ? next : latest)
            )
            .ToList();

        var tables = dedupedCells.Select(c => c.Id.Table).Distinct().ToList();
        var rowIds = dedupedCells.Select(c => c.Id.RowId).Distinct().ToList();

        // Every column of the touched rows, not just the pushed ones, so a new tombstone can
        // find the row's older cells. Values are never loaded, only compared by HLC and replaced.
        var storedCells = await AmberContext
            .SyncCells.Where(c =>
                c.UserId == userId && tables.Contains(c.Table) && rowIds.Contains(c.RowId)
            )
            .Select(c => new StoredCell(
                c.Table,
                c.RowId,
                c.Column,
                new Hlc(c.Hlc.Value),
                c.SizeInBytes
            ))
            .ToListAsync(cancellationToken);

        var storedByKey = storedCells.ToDictionary(c => (c.Table, c.RowId, c.Column));
        var tombstones = storedCells
            .Where(c => c.Column == SyncCell.DeletedColumn)
            .ToDictionary(c => (c.Table, c.RowId), c => c.Hlc);
        var writtenTombstones = new Dictionary<(string Table, string RowId), Hlc>();
        var writes = new List<(SyncCell Cell, bool IsNew)>();
        long storageDelta = 0;

        // Tombstones go first so a batch's own older cells for a deleted row are dropped too.
        foreach (
            var cell in dedupedCells.OrderByDescending(c => c.Id.Column == SyncCell.DeletedColumn)
        )
        {
            storedByKey.TryGetValue((cell.Id.Table, cell.Id.RowId, cell.Id.Column), out var stored);
            var rowKey = (cell.Id.Table, cell.Id.RowId);

            if (stored is not null && !cell.Hlc.IsAfter(stored.Hlc))
            {
                continue;
            }

            if (cell.Id.Column == SyncCell.DeletedColumn)
            {
                tombstones[rowKey] = cell.Hlc;
                writtenTombstones[rowKey] = cell.Hlc;
            }
            else if (
                tombstones.TryGetValue(rowKey, out var tombstone) && !cell.Hlc.IsAfter(tombstone)
            )
            {
                // Older than the row's delete: storing it would keep a dead payload around.
                continue;
            }

            var written = new SyncCell(
                new SyncCellId(userId, cell.Id.Table, cell.Id.RowId, cell.Id.Column),
                cell.Value,
                cell.Hlc,
                cell.DeviceId
            );
            writes.Add((written, stored is null));
            storageDelta += written.SizeInBytes - (stored?.SizeInBytes ?? 0);
        }

        if (writes.Count == 0)
        {
            return true;
        }

        // A deleted row only needs its tombstone, so its older cells (e.g. a whole PDF) are
        // dropped instead of being stored and re-sent to every pulling device. Newer cells stay,
        // since they resurrect the row; cells overwritten in this batch are newer by definition.
        var writtenKeys = writes
            .Select(w => (w.Cell.Table, w.Cell.RowId, w.Cell.Column))
            .ToHashSet();
        var staleCells = storedCells
            .Where(c =>
                c.Column != SyncCell.DeletedColumn
                && !writtenKeys.Contains((c.Table, c.RowId, c.Column))
                && writtenTombstones.TryGetValue((c.Table, c.RowId), out var tombstone)
                && !c.Hlc.IsAfter(tombstone)
            )
            .ToList();
        storageDelta -= staleCells.Sum(c => c.SizeInBytes);

        // Checked before writing so a rejected push never sends its payloads to the database.
        // A push that doesn't grow storage is always let through, even for a user over the limit.
        if (
            storageDelta > 0
            && await CalculateUserUsedStorageInBytesAsync(userId, cancellationToken) + storageDelta
                > maxStoragePerUserInBytes
        )
        {
            return false;
        }

        var serverSeq =
            await AmberContext
                .SyncCells.Where(c => c.UserId == userId)
                .Select(c => (long?)c.ServerSeq)
                .MaxAsync(cancellationToken) ?? 0;
        var writtenAt = DateTime.UtcNow;
        var trackedCells = new List<SyncCell>();

        foreach (var (cell, isNew) in writes)
        {
            cell.ServerSeq = ++serverSeq;
            cell.WrittenAt = writtenAt;

            // Update() on a fresh instance issues an UPDATE by key without loading the old row.
            if (isNew)
            {
                AmberContext.SyncCells.Add(cell);
            }
            else
            {
                AmberContext.SyncCells.Update(cell);
            }

            trackedCells.Add(cell);
        }

        foreach (var stale in staleCells)
        {
            var stub = new SyncCell(
                new SyncCellId(userId, stale.Table, stale.RowId, stale.Column),
                null,
                stale.Hlc,
                string.Empty
            );
            AmberContext.SyncCells.Remove(stub);
            trackedCells.Add(stub);
        }

        // One SaveChanges, so the upserts and deletes commit atomically.
        await AmberContext.SaveChangesAsync(cancellationToken);

        // Detached so a later push on the same context can attach its own instances for these keys.
        foreach (var cell in trackedCells)
        {
            AmberContext.Entry(cell).State = EntityState.Detached;
        }

        return true;
    }

    private record StoredCell(string Table, string RowId, string Column, Hlc Hlc, long SizeInBytes);

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
