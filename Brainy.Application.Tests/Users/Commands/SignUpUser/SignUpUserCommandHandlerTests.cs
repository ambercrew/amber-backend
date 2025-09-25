using Brainy.Application.Services;
using Brainy.Application.Users.Commands.SignUpUser;
using Brainy.Application.Users.Services;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Users.Repositories;
using Brainy.TestUtils;
using Brainy.TestUtils.Users;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace Brainy.Application.Tests.Users.Commands.SignUpUser;

[TestClass]
public class SignUpUserCommandHandlerTests : RepositoryTestBase
{
    private SignUpUserCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(BrainyContext);

        var randomGenerator = Substitute.For<IRandomGenerator>();
        randomGenerator
            .GenerateRandomAlphanumeric(EmailVerificationCode.MaxLength)
            .Returns("1234TEST");

        _handler = new SignUpUserCommandHandler(
            _userRepository,
            Substitute.For<IUserEmailService>(),
            randomGenerator,
            Substitute.For<ILogger<SignUpUserCommandHandler>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_EmailAlreadyUsed_ThrowsInvalidOperationException()
    {
        // Arrange

        var existingUser = UserTestUtils.CreateUser(
            "existing-user",
            email: new Email("email@email.com")
        );
        await _userRepository.AddAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        var command = new SignUpUserCommand(
            new SignUpDto("new-username", "testPassword123", "email@email.com", "First", "Last")
        );

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_UsernameAlreadyUsed_ThrowsInvalidOperationException()
    {
        // Arrange

        var existingUser = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        var command = new SignUpUserCommand(
            new SignUpDto("test-user", "testPassword123", "another@email.com", "First", "Last")
        );

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_StoresUserWithHashedPassword()
    {
        // Arrange

        var password = "testPassword123";
        var command = new SignUpUserCommand(
            new SignUpDto("test-user", password, "email@email.com", "First", "Last")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var user = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        BC.Verify(password, user.Password.Value).Should().BeTrue();
    }
}
