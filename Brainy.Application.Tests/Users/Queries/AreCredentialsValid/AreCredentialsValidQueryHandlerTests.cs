using Brainy.Application.Queries.AreCredentialsValid;
using Brainy.Application.Users.Queries.AreCredentialsValid;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Users.Repositories;
using Brainy.TestUtils;
using Brainy.TestUtils.Users;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Tests.Users.Queries.AreCredentialsValid;

[TestClass]
public class AreCredentialsValidQueryHandlerTests : RepositoryTestBase
{
    private AreCredentialsValidQueryHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(BrainyContext);
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
