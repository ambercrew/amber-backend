using Brainy.Domain.Common.Interfaces;
using Brainy.Domain.Sync.Entities;

namespace Brainy.Domain.Sync.Repositories;

public interface ISyncedEntityRepository : IUnitOfWorkRepository
{
    Task<IList<SyncedEntity>> GetSyncedEntitiesAfterOrderedByCreatedDateAsync(
        Guid userId,
        DateTime date,
        int skipCount,
        int takeCount,
        CancellationToken cancellationToken = default
    );

    Task UpsertRangeAsync(
        IList<SyncedEntity> Entities,
        CancellationToken cancellationToken = default
    );

    Task<long> CalculateUserUsedStorageInBytesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<long> CalculateStorageForEntitiesInBytesAsync(
        Guid userId,
        IList<Guid> entityIds,
        CancellationToken cancellationToken = default
    );
}
