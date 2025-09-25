using Brainy.Domain.Sync.Entities;
using Brainy.Domain.Sync.Repositories;
using Brainy.Infrastructure.Common;
using Brainy.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Brainy.Infrastructure.Sync.Repositories;

public class SyncedEntityRepository(BrainyContext brainyContext)
    : UnitOfWorkRepositoryBase(brainyContext),
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
            from syncedEntity in BrainyContext.SyncedEntities
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
        await using var transaction = await BrainyContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        await BrainyContext.SyncedEntities.UpsertRange(Entities).RunAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<long> CalculateUserUsedStorageInBytesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await BrainyContext
            .SyncedEntities.Where(e => e.UserId == userId)
            .SumAsync(e => e.SizeInBytes, cancellationToken);
    }

    public async Task<long> CalculateStorageForEntitiesInBytesAsync(
        Guid userId,
        IList<Guid> entityIds,
        CancellationToken cancellationToken = default
    )
    {
        return await BrainyContext
            .SyncedEntities.Where(e => e.UserId == userId && entityIds.Contains(e.EntityId))
            .SumAsync(e => e.SizeInBytes, cancellationToken);
    }
}
