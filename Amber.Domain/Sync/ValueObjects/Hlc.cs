using System.Globalization;
using System.Text.RegularExpressions;
using Amber.Domain.Common;

namespace Amber.Domain.Sync.ValueObjects;

/// <summary>
/// A Hybrid Logical Clock value, formatted as "{physical_ms}-{counter}-{device_id}".
/// Components are not zero-padded, so causal order must be compared component-wise
/// (see <see cref="IsAfter"/>) rather than via plain string comparison. Used as the
/// version for last-write-wins conflict resolution between devices.
/// </summary>
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
    /// Compares two HLC values component-wise (physical time, then counter, then
    /// device id) since the components are not zero-padded and so cannot be ordered
    /// correctly via plain string comparison.
    /// </summary>
    public bool IsAfter(Hlc other)
    {
        var (physical, counter, deviceId) = Parse(Value);
        var (otherPhysical, otherCounter, otherDeviceId) = Parse(other.Value);

        if (physical != otherPhysical)
        {
            return physical > otherPhysical;
        }

        if (counter != otherCounter)
        {
            return counter > otherCounter;
        }

        return string.CompareOrdinal(deviceId, otherDeviceId) > 0;
    }

    private static (ulong Physical, ulong Counter, string DeviceId) Parse(string value)
    {
        var parts = value.Split('-', 3);

        return (ulong.Parse(parts[0]), ulong.Parse(parts[1], NumberStyles.HexNumber), parts[2]);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
