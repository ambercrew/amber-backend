namespace Brainy.Application.Sync.Dto;

public record SyncEntityDto(Guid EntityId, DateTime CreatedDate, int EntityType, byte[] Data);
