using LiteBus.Queries.Abstractions;

namespace Amber.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

public record GetSyncedEntitiesAfterOrderedByCreatedDateQuery(DateTime Date, int Page, Guid UserId)
    : IQuery<SyncedEntitiesPageDto>;
