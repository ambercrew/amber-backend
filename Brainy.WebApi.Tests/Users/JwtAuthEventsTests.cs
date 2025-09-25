using System.Security.Claims;
using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using Brainy.WebApi.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Brainy.WebApi.Tests.Users;

[TestClass]
public class JwtAuthEventsTests
{
    private IUserRepository _userRepository = null!;
    private MockHttpContext _mockHttpContext = null!;
    private JwtAuthEvents _jwtAuthEvents = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _mockHttpContext = new();
        _jwtAuthEvents = new(_userRepository, Substitute.For<ILogger<JwtAuthEvents>>());
    }

    [TestMethod]
    public async Task TokenValidated_EmptyUsername_RequestRejected()
    {
        // Arrange

        var context = CreateContext();

        // Act

        await _jwtAuthEvents.TokenValidated(context);

        // Assert

        context.Result!.Failure.Should().NotBeNull();
    }

    [TestMethod]
    public async Task TokenValidated_NoIssuedDate_RequestRejected()
    {
        // Arrange

        var context = CreateContext("test-user");
        _userRepository
            .IsUsernameUsedAsync(new Username("test-user"))
            .Returns(Task.FromResult(true));
        _userRepository
            .GetUserSignOutDateTimeAsync(new Username("test-user"))
            .Returns(DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        // Act

        await _jwtAuthEvents.TokenValidated(context);

        // Assert

        context.Result!.Failure.Should().NotBeNull();
    }

    [TestMethod]
    public async Task TokenValidated_IssuedDateIsLessThanSignOutDate_RequestRejected()
    {
        // Arrange

        var context = CreateContext(
            "test-user",
            DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10))
        );
        _userRepository
            .IsUsernameUsedAsync(new Username("test-user"))
            .Returns(Task.FromResult(true));
        _userRepository
            .GetUserSignOutDateTimeAsync(new Username("test-user"))
            .Returns(DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        // Act

        await _jwtAuthEvents.TokenValidated(context);

        // Assert

        context.Result!.Failure.Should().NotBeNull();
    }

    [TestMethod]
    public async Task TokenValidated_ValidInput_RequestNotRejected()
    {
        // Arrange

        var context = CreateContext("test-user", DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)));
        _userRepository
            .IsUsernameUsedAsync(new Username("test-user"))
            .Returns(Task.FromResult(true));
        _userRepository
            .GetUserSignOutDateTimeAsync(new Username("test-user"))
            .Returns(DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        // Act

        await _jwtAuthEvents.TokenValidated(context);

        // Assert

        context.Result.Should().BeNull();
    }

    private TokenValidatedContext CreateContext(string username = "", DateTime? issuedAt = null)
    {
        IList<Claim> claims = [new Claim(ClaimTypes.Name, username)];

        if (issuedAt is not null)
        {
            claims.Add(new Claim(JwtTokenService.IssuedAtClaimType, issuedAt.Value.ToString("o")));
        }

        ClaimsIdentity claimsIdentity = new(claims, JwtBearerDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(claimsIdentity);

        var scheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            null,
            typeof(JwtBearerHandler)
        );

        return new TokenValidatedContext(_mockHttpContext, scheme, new JwtBearerOptions())
        {
            Principal = principal,
        };
    }
}
