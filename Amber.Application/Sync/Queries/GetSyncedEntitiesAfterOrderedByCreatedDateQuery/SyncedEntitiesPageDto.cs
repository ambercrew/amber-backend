using Amber.Domain.Sync.Entities;

namespace Amber.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

public record SyncedEntitiesPageDto(IList<SyncedEntity> SyncedEntities, bool HasMore);
