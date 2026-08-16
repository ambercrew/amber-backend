namespace Amber.Domain.Sync.Configurations;

public class SyncConfiguration
{
    public int SyncCellsPageSize { get; init; }
    public long MaxStoragePerUserInBytes { get; init; }
}
