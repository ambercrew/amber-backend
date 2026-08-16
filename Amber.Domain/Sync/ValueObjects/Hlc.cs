using System.Text.RegularExpressions;
using Amber.Domain.Common;

namespace Amber.Domain.Sync.ValueObjects;

/// <summary>
/// A Hybrid Logical Clock value, formatted as a fixed-width, lexicographically
/// sortable string ("{physical_ms}-{counter}-{device_id}") so that plain text
/// comparison/ordering (e.g. in SQL) matches causal order. Used as the version
/// for last-write-wins conflict resolution between devices.
/// </summary>
// TODO: unit test
public partial class Hlc : ValueObject
{
    [GeneratedRegex(@"^\d+-[0-9A-Fa-f]+-\S+$")]
    private static partial Regex HlcValidationRegex();

    public string Value { get; init; } = null!;

    // Private parameterless constructor for EF Core
    private Hlc() { }

    public Hlc(string value)
    {
        if (!HlcValidationRegex().IsMatch(value))
        {
            throw new InvalidOperationException(
                "The HLC value must be in the format \"{physical_ms}-{counter}-{device_id}\"."
            );
        }

        Value = value;
    }

    /// <summary>
    /// Compares two HLC values using ordinal string comparison, which matches their
    /// causal order because the numeric/hex components are zero-padded to a fixed width.
    /// </summary>
    public bool IsAfter(Hlc other) => string.CompareOrdinal(Value, other.Value) > 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
