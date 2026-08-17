using System.Text;
using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.ValueObjects;

namespace Amber.Domain.Tests.Sync.Entities;

[TestClass]
public class SyncCellTests
{
    [TestMethod]
    public void New_ValueIsNull_SizeInBytesExcludesValueLength()
    {
        // Arrange

        var id = new SyncCellId(Guid.NewGuid(), "table", "row", "column");
        var hlc = new Hlc("100-0-device");

        // Act

        var cell = new SyncCell(id, null, hlc, "device");

        // Assert

        cell.SizeInBytes.Should().Be(ExpectedSize(id, null, hlc, "device"));
    }

    [TestMethod]
    public void New_ValueIsSet_SizeInBytesIncludesValueLength()
    {
        // Arrange

        var id = new SyncCellId(Guid.NewGuid(), "table", "row", "column");
        var hlc = new Hlc("100-0-device");
        var value = Encoding.UTF8.GetBytes("some payload");

        // Act

        var cell = new SyncCell(id, value, hlc, "device");

        // Assert

        cell.SizeInBytes.Should().Be(ExpectedSize(id, value, hlc, "device"));
    }

    [TestMethod]
    public void Value_SetAfterConstruction_SizeInBytesRecomputed()
    {
        // Arrange

        var id = new SyncCellId(Guid.NewGuid(), "table", "row", "column");
        var hlc = new Hlc("100-0-device");
        var cell = new SyncCell(id, null, hlc, "device");
        var newValue = Encoding.UTF8.GetBytes("updated payload");

        // Act

        cell.Value = newValue;

        // Assert

        cell.SizeInBytes.Should().Be(ExpectedSize(id, newValue, hlc, "device"));
    }

    [TestMethod]
    public void Hlc_SetAfterConstruction_SizeInBytesRecomputed()
    {
        // Arrange

        var id = new SyncCellId(Guid.NewGuid(), "table", "row", "column");
        var hlc = new Hlc("100-0-device");
        var cell = new SyncCell(id, null, hlc, "device");
        var newHlc = new Hlc("200-0-another-device-id");

        // Act

        cell.Hlc = newHlc;

        // Assert

        cell.SizeInBytes.Should().Be(ExpectedSize(id, null, newHlc, "device"));
    }

    [TestMethod]
    public void DeviceId_SetAfterConstruction_SizeInBytesRecomputed()
    {
        // Arrange

        var id = new SyncCellId(Guid.NewGuid(), "table", "row", "column");
        var hlc = new Hlc("100-0-device");
        var cell = new SyncCell(id, null, hlc, "device");

        // Act

        cell.DeviceId = "a-much-longer-device-id";

        // Assert

        cell.SizeInBytes.Should().Be(ExpectedSize(id, null, hlc, "a-much-longer-device-id"));
    }

    private static long ExpectedSize(SyncCellId id, byte[]? value, Hlc hlc, string deviceId) =>
        16
        + Encoding.UTF8.GetByteCount(id.Table)
        + Encoding.UTF8.GetByteCount(id.RowId)
        + Encoding.UTF8.GetByteCount(id.Column)
        + (value?.LongLength ?? 0)
        + Encoding.UTF8.GetByteCount(hlc.Value)
        + Encoding.UTF8.GetByteCount(deviceId)
        + 8
        + SyncCell.StorageOverheadPerCellInBytes;
}
