using LiteBus.Queries.Abstractions;

namespace Brainy.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

public record GetSyncedEntitiesAfterOrderedByCreatedDateQuery(DateTime Date, int Page, Guid UserId)
    : IQuery<SyncedEntitiesPageDto>;
