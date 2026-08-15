using Amber.Domain.Users.ValueObjects;

namespace Amber.Domain.Tests.Users.ValueObjects;

[TestClass]
public class EmailTests
{
    [TestMethod]
    [DataRow("invalid email")]
    [DataRow("")]
    public void New_InvalidEmail_ExceptionThrown(string email)
    {
        // Act & Assert

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var _ = new Email(email);
        });
    }
}
