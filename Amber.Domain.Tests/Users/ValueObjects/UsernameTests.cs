using Amber.Domain.Users.ValueObjects;

namespace Amber.Domain.Tests.Users.ValueObjects;

[TestClass]
public class UsernameTests
{
    [TestMethod]
    [DataRow("-test-user")]
    [DataRow("aa")]
    [DataRow("")]
    public void New_InvalidUsername_ExceptionThrown(string username)
    {
        // Act & Assert

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var _ = new Username(username);
        });
    }
}
