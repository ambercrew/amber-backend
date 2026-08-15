using Amber.Application.Services;
using Amber.Application.Users.Commands.SignInWithGoogle;
using Amber.Application.Users.Services;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Tests.Users.Commands.SignInWithGoogle;

[TestClass]
public class SignInWithGoogleCommandHandlerTests : RepositoryTestBase
{
    private SignInWithGoogleCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;
    private IGoogleTokenVerifier _googleTokenVerifier = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _googleTokenVerifier = Substitute.For<IGoogleTokenVerifier>();

        var randomGenerator = Substitute.For<IRandomGenerator>();
        randomGenerator.GenerateRandomAlphanumeric(Arg.Any<int>()).Returns("1234TEST");

        _handler = new SignInWithGoogleCommandHandler(
            _userRepository,
            _googleTokenVerifier,
            randomGenerator,
            Substitute.For<ILogger<SignInWithGoogleCommandHandler>>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_InvalidToken_ThrowsInvalidOperationException()
    {
        // Arrange

        _googleTokenVerifier.VerifyAsync("bad-token").Returns((GoogleUserInfo?)null);

        var command = new SignInWithGoogleCommand(new GoogleSignInDto("bad-token"));

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_GoogleIdAlreadyLinked_ReturnsExistingUser()
    {
        // Arrange

        var existingUser = UserTestUtils.CreateUser(
            "test-user",
            hasPassword: false,
            googleId: "google-id"
        );
        await _userRepository.AddAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        _googleTokenVerifier
            .VerifyAsync("token")
            .Returns(new GoogleUserInfo("google-id", "other@email.com", "First", "Last"));

        var command = new SignInWithGoogleCommand(new GoogleSignInDto("token"));

        // Act

        var result = await _handler.HandleAsync(command);

        // Assert

        result.Id.Should().Be(existingUser.Id);
    }

    [TestMethod]
    public async Task HandleAsync_EmailMatchesExistingVerifiedPasswordUser_LinksGoogleAccount()
    {
        // Arrange

        var existingUser = UserTestUtils.CreateUser(
            "test-user",
            email: new Email("shared@email.com"),
            isEmailVerified: true
        );
        await _userRepository.AddAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        _googleTokenVerifier
            .VerifyAsync("token")
            .Returns(new GoogleUserInfo("google-id", "shared@email.com", "First", "Last"));

        var command = new SignInWithGoogleCommand(new GoogleSignInDto("token"));

        // Act

        var result = await _handler.HandleAsync(command);

        // Assert

        result.Id.Should().Be(existingUser.Id);
        result.IsEmailVerified.Should().BeTrue();

        var linkedUser = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        linkedUser.GoogleId.Should().Be("google-id");
    }

    [TestMethod]
    public async Task HandleAsync_EmailMatchesExistingUnverifiedPasswordUser_ThrowsInvalidOperationException()
    {
        // Arrange

        var existingUser = UserTestUtils.CreateUser(
            "test-user",
            email: new Email("shared@email.com"),
            isEmailVerified: false
        );
        await _userRepository.AddAsync(existingUser);
        await _userRepository.SaveChangesAsync();

        _googleTokenVerifier
            .VerifyAsync("token")
            .Returns(new GoogleUserInfo("google-id", "shared@email.com", "First", "Last"));

        var command = new SignInWithGoogleCommand(new GoogleSignInDto("token"));

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );

        var untouchedUser = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        untouchedUser.GoogleId.Should().BeNull();
    }

    [TestMethod]
    public async Task HandleAsync_NewEmail_CreatesVerifiedUserWithoutPassword()
    {
        // Arrange

        _googleTokenVerifier
            .VerifyAsync("token")
            .Returns(new GoogleUserInfo("google-id", "new-user@email.com", "First", "Last"));

        var command = new SignInWithGoogleCommand(new GoogleSignInDto("token"));

        // Act

        var result = await _handler.HandleAsync(command);

        // Assert

        result.Email.Should().Be("new-user@email.com");
        result.IsEmailVerified.Should().BeTrue();

        var createdUser = await _userRepository.GetUserByEmailIfExistsAsync(
            new Email("new-user@email.com")
        );
        createdUser.Should().NotBeNull();
        createdUser!.GoogleId.Should().Be("google-id");
        createdUser.Password.Should().BeNull();
    }
}
