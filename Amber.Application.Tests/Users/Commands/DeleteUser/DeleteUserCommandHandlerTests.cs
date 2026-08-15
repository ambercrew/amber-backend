using Amber.Application.Users.Commands.DeleteUser;
using Amber.Application.Users.Services;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Tests.Users.Commands.DeleteUser;

[TestClass]
public class DeleteUserCommandHandlerTests : RepositoryTestBase
{
    private DeleteUserCommandHandler _handler = null!;
    private IUserDeletionEmailSender _deletionEmailSender = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _deletionEmailSender = Substitute.For<IUserDeletionEmailSender>();
        _handler = new DeleteUserCommandHandler(
            _userRepository,
            _deletionEmailSender,
            Substitute.For<ILogger<DeleteUserCommandHandler>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_UserDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange

        var command = new DeleteUserCommand(Guid.NewGuid());

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_DeletesUser()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new DeleteUserCommand(user.Id);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var deletedUser = await _userRepository.GetUserByUsernameIfExistsAsync(user.Username);
        deletedUser.Should().BeNull();

        await _deletionEmailSender.Received().SendDeletionEmailAsync(user);
    }
}
