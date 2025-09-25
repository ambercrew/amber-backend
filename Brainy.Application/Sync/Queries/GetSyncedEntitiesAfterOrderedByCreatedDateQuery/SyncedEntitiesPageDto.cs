using Brainy.Domain.Sync.Entities;

namespace Brainy.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

public record SyncedEntitiesPageDto(IList<SyncedEntity> SyncedEntities, bool HasMore);
