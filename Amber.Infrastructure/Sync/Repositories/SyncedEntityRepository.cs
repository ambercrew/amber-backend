using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Infrastructure.Common;
using Amber.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Amber.Infrastructure.Sync.Repositories;

public class SyncedEntityRepository(AmberContext amberContext)
    : UnitOfWorkRepositoryBase(amberContext),
        ISyncedEntityRepository
{
    public async Task<IList<SyncedEntity>> GetSyncedEntitiesAfterOrderedByCreatedDateAsync(
        Guid userId,
        DateTime date,
        int skipCount,
        int takeCount,
        CancellationToken cancellationToken = default
    )
    {
        return await (
            from syncedEntity in AmberContext.SyncedEntities
            where syncedEntity.UserId == userId && date < syncedEntity.LastSyncDate
            // Ordering by id after created date for entities with same created date.
            orderby syncedEntity.CreatedDate, syncedEntity.EntityId
            select syncedEntity
        )
            .Skip(skipCount)
            .Take(takeCount)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertRangeAsync(
        IList<SyncedEntity> Entities,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = await AmberContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        await AmberContext.SyncedEntities.UpsertRange(Entities).RunAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<long> CalculateUserUsedStorageInBytesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await AmberContext
            .SyncedEntities.Where(e => e.UserId == userId)
            .SumAsync(e => e.SizeInBytes, cancellationToken);
    }

    public async Task<long> CalculateStorageForEntitiesInBytesAsync(
        Guid userId,
        IList<Guid> entityIds,
        CancellationToken cancellationToken = default
    )
    {
        return await AmberContext
            .SyncedEntities.Where(e => e.UserId == userId && entityIds.Contains(e.EntityId))
            .SumAsync(e => e.SizeInBytes, cancellationToken);
    }
}
