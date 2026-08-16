using Amber.Domain.Common;

namespace Amber.Domain.Sync.ValueObjects;

/// <summary>
/// Identifies a single sync cell: the (user, table, row, column) tuple a cell's value belongs to.
/// </summary>
public class SyncCellId : ValueObject
{
    public Guid UserId { get; init; }

    public string Table { get; init; } = null!;

    public string RowId { get; init; } = null!;

    public string Column { get; init; } = null!;

    // Private parameterless constructor for EF Core
    private SyncCellId() { }

    public SyncCellId(Guid userId, string table, string rowId, string column)
    {
        UserId = userId;
        Table = table;
        RowId = rowId;
        Column = column;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return UserId;
        yield return Table;
        yield return RowId;
        yield return Column;
    }
}
