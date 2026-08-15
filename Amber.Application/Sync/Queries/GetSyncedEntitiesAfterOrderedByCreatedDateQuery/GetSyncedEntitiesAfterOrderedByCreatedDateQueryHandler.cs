using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Repositories;
using LiteBus.Queries.Abstractions;

namespace Amber.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

public class GetSyncedEntitiesAfterOrderedByCreatedDateQueryHandler(
    ISyncedEntityRepository syncedEntityRepository,
    SyncConfiguration syncConfiguration
) : IQueryHandler<GetSyncedEntitiesAfterOrderedByCreatedDateQuery, SyncedEntitiesPageDto>
{
    public async Task<SyncedEntitiesPageDto> HandleAsync(
        GetSyncedEntitiesAfterOrderedByCreatedDateQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var skipCount = query.Page * syncConfiguration.SyncedEntitiesPageSize;
        var entities = await syncedEntityRepository.GetSyncedEntitiesAfterOrderedByCreatedDateAsync(
            query.UserId,
            query.Date,
            skipCount,
            syncConfiguration.SyncedEntitiesPageSize,
            cancellationToken
        );
        var hasMore = entities.Count == syncConfiguration.SyncedEntitiesPageSize;
        return new SyncedEntitiesPageDto(entities, hasMore);
    }
}
