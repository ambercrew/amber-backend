using Amber.Application.Users.Commands.VerifyUserEmail;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Tests.Users.Commands.VerifyUserEmail;

[TestClass]
public class VerifyUserEmailCommandHandlerTests : RepositoryTestBase
{
    private VerifyUserEmailCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _handler = new VerifyUserEmailCommandHandler(
            _userRepository,
            Substitute.For<ILogger<VerifyUserEmailCommand>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_EmailAlreadyVerified_ThrowsInvalidOperationException()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user", isEmailVerified: true);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new VerifyUserEmailCommand(
            new Username("test-user"),
            new VerifyEmailDto("12345678")
        );

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_IncorrectCode_ThrowsInvalidOperationException()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new VerifyUserEmailCommand(
            new Username("test-user"),
            new VerifyEmailDto("WRONG123") // UserTestUtils default code is "12345678"
        );

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_CorrectCode_VerifiesUserEmail()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new VerifyUserEmailCommand(
            new Username("test-user"),
            new VerifyEmailDto("12345678") // matches UserTestUtils default code
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var updatedUser = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        updatedUser.IsEmailVerified.Should().BeTrue();
    }
}
