namespace Brainy.Domain.Sync.Configurations;

public class SyncConfiguration
{
    public int SyncedEntitiesPageSize { get; init; }
    public long MaxStoragePerUserInBytes { get; init; }
}
