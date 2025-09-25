using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Brainy.Application.Queries.AreCredentialsValid;
using Brainy.Application.Users.Commands.ResendEmailVerificationCode;
using Brainy.Application.Users.Commands.SignOutUser;
using Brainy.Application.Users.Commands.SignUpUser;
using Brainy.Application.Users.Commands.UpdatePassword;
using Brainy.Application.Users.Commands.VerifyUserEmail;
using Brainy.Application.Users.Dto;
using Brainy.Application.Users.Queries.AreCredentialsValid;
using Brainy.Application.Users.Queries.GetUserByUsername;
using Brainy.Domain.Users.ValueObjects;
using Brainy.TestUtils.Users;
using Brainy.WebApi.Configurations;
using Brainy.WebApi.Users;
using Brainy.WebApi.Users.Dto;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace Brainy.WebApi.Tests.Users;

[TestClass]
public class AuthControllerTests
{
    private MockHttpContext _mockHttpContext = null!;
    private ICommandMediator _commandMediator = null!;
    private IQueryMediator _queryMediator = null!;
    private AuthController _controller = null!;

    [TestInitialize]
    public void Initialize()
    {
        _commandMediator = Substitute.For<ICommandMediator>();
        _queryMediator = Substitute.For<IQueryMediator>();
        var jwtTokenService = new JwtTokenService(
            new JwtConfiguration
            {
                SecretKey = "unit-test-secret-key-unit-test-secret-key",
                Issuer = "Brainy.Tests",
                Audience = "Brainy.Tests",
                ExpiryInDays = 60,
            }
        );
        _controller = new(
            Substitute.For<ILogger<AuthController>>(),
            _commandMediator,
            _queryMediator,
            jwtTokenService
        );
        _mockHttpContext = new();
        _controller.ControllerContext.HttpContext = _mockHttpContext;
    }

    [TestMethod]
    public async Task SignInAsync_InvalidCredentials_ReturnedUnauthorized()
    {
        // Arrange

        var input = new SignInDto("username", "testPassword123");
        _queryMediator.QueryAsync(new AreCredentialsValidQuery(input)).Returns(false);

        // Act

        var actual = (IStatusCodeActionResult)await _controller.SignInAsync(input);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [TestMethod]
    public async Task SignInAsync_ValidCredentials_AuthorizedRequest()
    {
        // Arrange

        var input = new SignInDto("test-user", "testPassword123");
        _queryMediator.QueryAsync(new AreCredentialsValidQuery(input)).Returns(true);

        var user = UserTestUtils.CreateUser(input.Username);

        _queryMediator
            .QueryAsync(new GetUserByUsernameQuery(new Username(input.Username)))
            .Returns(UserInformationDto.FromUser(user));

        // Act

        var actual = (OkObjectResult)await _controller.SignInAsync(input);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = (SignInResponseDto)actual.Value!;
        response.User.Should().Be(UserInformationDto.FromUser(user));
        ValidateToken(response.Token, "test-user", user.Id);
    }

    [TestMethod]
    public async Task SignUpAsync_ValidInput_UserSignedUp()
    {
        // Arrange

        var input = new SignUpDto(
            "test-user",
            "testPassword123",
            "email@test.com",
            "first name",
            "last name"
        );
        var user = UserTestUtils.CreateUser(input.Username);

        _commandMediator
            .SendAsync(new SignUpUserCommand(input))
            .Returns(UserInformationDto.FromUser(user));

        // Act

        var actual = (OkObjectResult)await _controller.SignUpAsync(input);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = (SignInResponseDto)actual.Value!;
        response.User.Should().Be(UserInformationDto.FromUser(user));
        ValidateToken(response.Token, "test-user", user.Id);

        await _commandMediator.Received().SendAsync(new SignUpUserCommand(input));
    }

    [TestMethod]
    public async Task UpdatePasswordAsync_ValidInput_UpdatedPassword()
    {
        // Arrange

        var input = new UpdatePasswordDto("oldPassword123", "newPassword123");
        var user = UserTestUtils.CreateUser("test-user");
        _commandMediator
            .SendAsync(new UpdatePasswordCommand(input, user.Username))
            .Returns(UserInformationDto.FromUser(user));
        _mockHttpContext.SetupSignedUser(user.Username);

        // Act

        var actual = (OkObjectResult)await _controller.UpdatePasswordAsync(input);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = (SignInResponseDto)actual.Value!;
        ValidateToken(response.Token, "test-user", user.Id);

        await _commandMediator
            .Received()
            .SendAsync(new UpdatePasswordCommand(input, user.Username));
    }

    [TestMethod]
    public async Task VerifyUserEmailAsync_ValidInput_VerifiedUser()
    {
        // Arrange

        var input = new VerifyEmailDto("12345678");

        _mockHttpContext.SetupSignedUser(new Username("test-user"));

        var user = UserTestUtils.CreateUser("test-user", isEmailVerified: true);
        _queryMediator
            .QueryAsync(new GetUserByUsernameQuery(user.Username))
            .Returns(UserInformationDto.FromUser(user));

        // Act

        var actual = (OkObjectResult)await _controller.VerifyUserEmailAsync(input);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        var response = (SignInResponseDto)actual.Value!;
        ValidateToken(response.Token, "test-user", user.Id, true);

        await _commandMediator
            .Received()
            .SendAsync(new VerifyUserEmailCommand(user.Username, input));
    }

    [TestMethod]
    public async Task VerifyUserEmailAsync_ExceptionThrown_DoesNotCatchException()
    {
        // Arrange

        VerifyEmailDto input = new("12345678");

        _mockHttpContext.SetupSignedUser(new Username("test-user"));

        var user = UserTestUtils.CreateUser("test-user", isEmailVerified: true);
        _queryMediator
            .QueryAsync(new GetUserByUsernameQuery(user.Username))
            .Returns(UserInformationDto.FromUser(user));

        _commandMediator
            .SendAsync(new VerifyUserEmailCommand(user.Username, input))
            .ThrowsAsync(new InvalidOperationException());

        // Act & Assert

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _controller.VerifyUserEmailAsync(input);
        });
    }

    [TestMethod]
    public async Task ResendEmailVerificationCodeAsync_ValidInput_SentEmail()
    {
        // Arrange

        _mockHttpContext.SetupSignedUser(new Username("test-user"));

        // Act

        var actual = (IStatusCodeActionResult)await _controller.ResendEmailVerificationCodeAsync();

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        await _commandMediator
            .Received()
            .SendAsync(new ResendEmailVerificationCodeCommand(new Username("test-user")));
    }

    [TestMethod]
    public async Task SignOutAsync_ValidInput_UserSignedOut()
    {
        // Arrange

        _mockHttpContext.SetupSignedUser(new Username("test-user"));
        var user = UserTestUtils.CreateUser("test-user");

        // Act

        var actual = (IStatusCodeActionResult)await _controller.SignOutAsync();

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        await _commandMediator.Received().SendAsync(new SignOutUserCommand(user.Username));
    }

    private static void ValidateToken(
        string token,
        string username,
        Guid userId,
        bool isVerifiedUser = false
    )
    {
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwtToken.Claims;

        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
        claims
            .Should()
            .Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        claims.Should().Contain(c => c.Type == JwtTokenService.IssuedAtClaimType);

        if (isVerifiedUser)
        {
            claims
                .Should()
                .Contain(c => c.Type == ClaimTypes.Role && c.Value == Constants.Roles.VerifiedUser);
        }
        else
        {
            claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
        }

        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow.AddDays(59));
    }
}
