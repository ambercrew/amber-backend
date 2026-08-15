using Amber.Application.Users.Commands.UpdatePassword;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace Amber.Application.Tests.Users.Commands.UpdatePassword;

[TestClass]
public class UpdatePasswordCommandHandlerTests : RepositoryTestBase
{
    private UpdatePasswordCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _handler = new UpdatePasswordCommandHandler(
            _userRepository,
            Substitute.For<ILogger<UpdatePasswordCommandHandler>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_IncorrectOldPassword_ThrowsInvalidOperationException()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new UpdatePasswordCommand(
            new UpdatePasswordDto("wrongPassword123", "newPassword123"),
            new Username("test-user")
        );

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_UpdatesPasswordAndSignOutDate()
    {
        // Arrange

        var user = UserTestUtils.CreateUser(
            "test-user",
            signOutDate: DateTime.UtcNow - TimeSpan.FromMinutes(10)
        );
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new UpdatePasswordCommand(
            new UpdatePasswordDto("testPassword123", "newPassword123"),
            new Username("test-user")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var updatedUser = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        BC.Verify("newPassword123", updatedUser.Password.Value).Should().BeTrue();
        updatedUser.SignOutDate.Should().BeWithin(10.Seconds()).Before(DateTime.UtcNow);
    }
}
