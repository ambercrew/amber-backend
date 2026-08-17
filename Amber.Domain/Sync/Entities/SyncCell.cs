using System.Text;
using Amber.Domain.Sync.ValueObjects;

namespace Amber.Domain.Sync.Entities;

/// <summary>
/// A single synced cell: one column's value for one row of one table, owned by one user.
/// Column "__deleted" marks a tombstone (the row was deleted); column "__row" carries a
/// whole row as a JSON payload for tables synced at row granularity. Conflicts between
/// devices are resolved last-write-wins by comparing <see cref="Hlc"/>.
/// </summary>
public class SyncCell
{
    /// <summary>
    /// Estimated storage overhead per cell beyond the raw field data,
    /// accounting for row metadata and internal bookkeeping used by the database engine.
    /// </summary>
    public const long StorageOverheadPerCellInBytes = 32;

    public const string DeletedColumn = "__deleted";
    public const string RowColumn = "__row";

    // Private parameterless constructor for EF Core
    private SyncCell() { }

    public SyncCell(SyncCellId id, byte[]? value, Hlc hlc, string deviceId)
    {
        UserId = id.UserId;
        Table = id.Table;
        RowId = id.RowId;
        Column = id.Column;
        // Assigning through the setters keeps SizeInBytes correct; Value is set first so the
        // (harmless) intermediate recomputation it triggers doesn't need Hlc/DeviceId yet.
        Value = value;
        Hlc = hlc;
        DeviceId = deviceId;
    }

    public Guid UserId { get; init; }

    public string Table { get; init; } = null!;

    public string RowId { get; init; } = null!;

    public string Column { get; init; } = null!;

    /// <summary>
    /// The (user, table, row, column) tuple that identifies this cell. Not its own mapped
    /// column - EF Core doesn't support composite/complex properties participating in a key,
    /// so <see cref="UserId"/>/<see cref="Table"/>/<see cref="RowId"/>/<see cref="Column"/>
    /// remain the actual mapped key columns and this just bundles them for domain code.
    /// </summary>
    public SyncCellId Id => new(UserId, Table, RowId, Column);

    public byte[]? Value
    {
        get;
        set
        {
            field = value;
            SizeInBytes = ComputeSize();
        }
    }

    public Hlc Hlc
    {
        get;
        set
        {
            field = value;
            SizeInBytes = ComputeSize();
        }
    } = null!;

    public string DeviceId
    {
        get;
        set
        {
            field = value;
            SizeInBytes = ComputeSize();
        }
    } = null!;

    /// <summary>
    /// Server-assigned, monotonically increasing (per user) sequence number, used as the
    /// pull cursor. Only advances when a cell is actually written (i.e. it wins the LWW
    /// conflict check), not on every push attempt.
    /// </summary>
    public long ServerSeq { get; set; }

    /// <summary>
    /// UTC timestamp of the last time this cell was written (inserted or updated by a
    /// winning LWW write). Used to determine per-user sync activity, e.g. for inactive
    /// user cleanup.
    /// </summary>
    public DateTime WrittenAt { get; set; }

    /// <summary>
    /// Estimated total storage size of this cell in bytes, including all fields
    /// and database storage overhead.
    /// </summary>
    public long SizeInBytes { get; private set; }

    // Table/RowId/Column can transiently be null here (before ComputeSize's first real call
    // from a property setter) only while EF Core is materializing an entity via the
    // parameterless constructor and hasn't yet run every property setter.
    private long ComputeSize() =>
        16
        + // UserId (Guid)
        Encoding.UTF8.GetByteCount(Table ?? string.Empty)
        + Encoding.UTF8.GetByteCount(RowId ?? string.Empty)
        + Encoding.UTF8.GetByteCount(Column ?? string.Empty)
        + (Value?.LongLength ?? 0)
        + Encoding.UTF8.GetByteCount(Hlc?.Value ?? string.Empty)
        + Encoding.UTF8.GetByteCount(DeviceId ?? string.Empty)
        + 8
        + // ServerSeq (long)
        StorageOverheadPerCellInBytes;
}
