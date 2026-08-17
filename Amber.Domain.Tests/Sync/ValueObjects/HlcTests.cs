using Amber.Domain.Sync.ValueObjects;

namespace Amber.Domain.Tests.Sync.ValueObjects;

[TestClass]
public class HlcTests
{
    [TestMethod]
    [DataRow("invalid")]
    [DataRow("")]
    [DataRow("123-abc")]
    [DataRow("123- 1-device")]
    public void New_InvalidValue_ExceptionThrown(string value)
    {
        // Act & Assert

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var _ = new Hlc(value);
        });
    }

    [TestMethod]
    [DataRow("123-1-device")]
    [DataRow("0-0-a")]
    [DataRow("999999999999-ff-device-with-dashes")]
    public void New_ValidValue_ValueIsSet(string value)
    {
        // Act

        var hlc = new Hlc(value);

        // Assert

        hlc.Value.Should().Be(value);
    }

    [TestMethod]
    public void IsAfter_LaterValue_ReturnsTrue()
    {
        // Arrange

        var earlier = new Hlc("100-0-device");
        var later = new Hlc("200-0-device");

        // Act

        var actual = later.IsAfter(earlier);

        // Assert

        actual.Should().BeTrue();
    }

    [TestMethod]
    public void IsAfter_EarlierValue_ReturnsFalse()
    {
        // Arrange

        var earlier = new Hlc("100-0-device");
        var later = new Hlc("200-0-device");

        // Act

        var actual = earlier.IsAfter(later);

        // Assert

        actual.Should().BeFalse();
    }

    [TestMethod]
    public void IsAfter_SameValue_ReturnsFalse()
    {
        // Arrange

        var first = new Hlc("100-0-device");
        var second = new Hlc("100-0-device");

        // Act

        var actual = first.IsAfter(second);

        // Assert

        actual.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange

        var first = new Hlc("100-0-device");
        var second = new Hlc("100-0-device");

        // Act

        var actual = first.Equals(second);

        // Assert

        actual.Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange

        var first = new Hlc("100-0-device");
        var second = new Hlc("200-0-device");

        // Act

        var actual = first.Equals(second);

        // Assert

        actual.Should().BeFalse();
    }
}
