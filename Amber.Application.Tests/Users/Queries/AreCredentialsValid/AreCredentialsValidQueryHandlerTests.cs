using Amber.Application.Queries.AreCredentialsValid;
using Amber.Application.Users.Queries.AreCredentialsValid;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Tests.Users.Queries.AreCredentialsValid;

[TestClass]
public class AreCredentialsValidQueryHandlerTests : RepositoryTestBase
{
    private AreCredentialsValidQueryHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _handler = new AreCredentialsValidQueryHandler(
            _userRepository,
            Substitute.For<ILogger<AreCredentialsValidQueryHandler>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange

        var query = new AreCredentialsValidQuery(
            new SignInDto("nonexistent-user", "testPassword123")
        );

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task HandleAsync_IncorrectPassword_ReturnsFalse()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var query = new AreCredentialsValidQuery(new SignInDto("test-user", "wrongPassword123"));

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task HandleAsync_UserHasNoPassword_ReturnsFalse()
    {
        // Arrange

        var user = UserTestUtils.CreateUser(
            "google-user",
            hasPassword: false,
            googleId: "google-id"
        );
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var query = new AreCredentialsValidQuery(new SignInDto("google-user", "testPassword123"));

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task HandleAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var query = new AreCredentialsValidQuery(
            new SignInDto("test-user", "testPassword123") // matches UserTestUtils default password
        );

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Should().BeTrue();
    }
}
