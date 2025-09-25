using System.Net;
using System.Net.Http.Json;
using Brainy.Application.Queries.AreCredentialsValid;
using Brainy.Application.Users.Commands.UpdatePassword;

namespace Brainy.WebApi.IntegrationTests.Users.Controllers;

[TestClass]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task CannotAcceptTokenAfterSignOut()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(HttpClient);
        await authenticatedHttpClient.SignInAsync();

        // Act

        // Waiting 2 seconds since the issued at is one second after the sign-in date.
        Thread.Sleep(2000);

        var firstSignOutResponse = await authenticatedHttpClient.PostAsync("api/v1/auth/sign-out");
        firstSignOutResponse.EnsureSuccessStatusCode();
        // AuthenticatedHttpClient will use the same token!
        var secondSignOutResponse = await authenticatedHttpClient.PostAsync(
            "api/v1/auth/sign-out",
            canSignIn: false
        );

        // Assert

        secondSignOutResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CannotLogInWithOldPasswordAfterChange()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(HttpClient);
        await authenticatedHttpClient.SignInAsync();

        // Act

        var updatePasswordResponse = await authenticatedHttpClient.PostAsync(
            "api/v1/auth/update-password",
            new UpdatePasswordDto(authenticatedHttpClient.Password, "NewPassword123")
        );
        updatePasswordResponse.EnsureSuccessStatusCode();

        // Assert

        var signInResponseWithOldPassword = await HttpClient.PostAsJsonAsync(
            "api/v1/auth/sign-in",
            new SignInDto(authenticatedHttpClient.Username, authenticatedHttpClient.Password)
        );
        signInResponseWithOldPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var signInResponseWithNewPassword = await HttpClient.PostAsJsonAsync(
            "api/v1/auth/sign-in",
            new SignInDto(authenticatedHttpClient.Username, "NewPassword123")
        );
        signInResponseWithNewPassword.EnsureSuccessStatusCode();
    }
}
