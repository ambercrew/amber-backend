using Amber.Application.Services;
using Amber.Application.Users.Commands.SignUpUser;
using Amber.Application.Users.Services;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace Amber.Application.Tests.Users.Commands.SignUpUser;

[TestClass]
public class SignUpUserCommandHandlerTests : RepositoryTestBase
{
    private SignUpUserCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);

        var randomGenerator = Substitute.For<IRandomGenerator>();
        randomGenerator
            .GenerateRandomAlphanumeric(EmailVerificationCode.MaxLength)
            .Returns("1234TEST");

        _handler = new SignUpUserCommandHandler(
            _userRepository,
            Substitute.For<IUserEmailVerificationCodeSender>(),
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
