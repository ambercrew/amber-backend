namespace Amber.Domain.Sync.Entities;

public class SyncedEntity(
    Guid userId,
    Guid entityId,
    DateTime createdDate,
    DateTime lastSyncDate,
    int entityType,
    byte[] data
)
{
    /// <summary>
    /// Estimated storage overhead per entity beyond the raw field data,
    /// accounting for row metadata and internal bookkeeping used by the database engine.
    /// </summary>
    public const long StorageOverheadPerEntityInBytes = 32;

    public Guid UserId { get; init; } = userId;

    public Guid EntityId { get; init; } = entityId;

    public DateTime CreatedDate { get; set; } = createdDate;

    public DateTime LastSyncDate { get; set; } = lastSyncDate;

    public int EntityType { get; set; } = entityType;

    public byte[] Data
    {
        get;
        set
        {
            field = value;
            SizeInBytes = ComputeSize(value);
        }
    } = data;

    /// <summary>
    /// Estimated total storage size of this entity in bytes, including all fields
    /// and database storage overhead.
    /// </summary>
    public long SizeInBytes { get; private set; } = ComputeSize(data);

    private static long ComputeSize(byte[] data) =>
        16
        + // UserId (Guid)
        16
        + // EntityId (Guid)
        8
        + // CreatedDate (DateTime)
        8
        + // LastSyncDate (DateTime)
        4
        + // EntityType (int)
        (data?.LongLength ?? 0)
        + // Data (byte[])
        StorageOverheadPerEntityInBytes;
}
