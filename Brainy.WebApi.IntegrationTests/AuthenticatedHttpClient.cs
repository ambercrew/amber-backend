using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Brainy.Application.Queries.AreCredentialsValid;
using Brainy.Application.Users.Commands.SignUpUser;
using Brainy.Application.Users.Commands.VerifyUserEmail;
using Brainy.WebApi.IntegrationTests.Services;
using Brainy.WebApi.Users.Dto;

namespace Brainy.WebApi.IntegrationTests;

/// <summary>
/// Used to send authenticated requests by signing-up a user and then logging-in
/// and storing the authentication token.
/// </summary>
public class AuthenticatedHttpClient
{
    private const int UsernameLength = 15;

    private HttpClient HttpClient { get; init; }
    private string? _token;
    public string Username { get; private init; }
    public string Password { get; private init; }

    public static async Task<AuthenticatedHttpClient> CreateAsync(
        HttpClient httpClient,
        bool verifyEmail = false
    )
    {
        var username = GenerateUniqueUsername();
        var registrationDto = new SignUpDto(
            username,
            "testPassword123",
            $"{username}@test.com",
            "first name",
            "last name"
        );
        var response = await httpClient.PostAsJsonAsync("/api/v1/auth/sign-up", registrationDto);
        response.EnsureSuccessStatusCode();

        AuthenticatedHttpClient client = new(
            httpClient,
            registrationDto.Username,
            registrationDto.Password
        );

        if (verifyEmail)
        {
            var verifyEmailResponse = await client.PostAsync(
                "/api/v1/auth/verify-email",
                new VerifyEmailDto(TestRandomGenerator.ReturnValue)
            );
            verifyEmailResponse.EnsureSuccessStatusCode();
        }

        return client;
    }

    private static string GenerateUniqueUsername()
    {
        StringBuilder stringBuilder = new(UsernameLength);
        for (var i = 0; i < UsernameLength; ++i)
        {
            var nextCharIndex = Random.Shared.Next(0, 26);
            var nextChar = (char)('a' + nextCharIndex);
            stringBuilder.Append(nextChar);
        }
        return stringBuilder.ToString();
    }

    private AuthenticatedHttpClient(HttpClient httpClient, string username, string password)
    {
        HttpClient = httpClient;
        Username = username;
        Password = password;
    }

    public Task<HttpResponseMessage> GetAsync(string uri) => SendAsync("GET", uri);

    public Task<HttpResponseMessage> PostAsync(
        string uri,
        object? body = null,
        bool canSignIn = true,
        BodyType bodyType = BodyType.Json
    ) => SendAsync("POST", uri, body, canSignIn, bodyType);

    public Task<HttpResponseMessage> PutAsync(string uri, object? body = null) =>
        SendAsync("PUT", uri, body);

    public Task<HttpResponseMessage> PatchAsync(string uri, object? body = null) =>
        SendAsync("PATCH", uri, body);

    public Task<HttpResponseMessage> DeleteAsync(string uri) => SendAsync("DELETE", uri);

    public async Task<HttpResponseMessage> SendAsync(
        string httpMethod,
        string uri,
        object? body = null,
        bool canSignIn = true,
        BodyType bodyType = BodyType.Json
    )
    {
        if (_token is null && canSignIn)
        {
            await SignInAsync();
        }

        HttpRequestMessage request = new(new HttpMethod(httpMethod), uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body is not null)
        {
            if (bodyType == BodyType.Json)
                request.Content = JsonContent.Create(body);
            else if (bodyType == BodyType.Bytes)
            {
                var content = new ByteArrayContent((byte[])body);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                request.Content = content;
            }
        }
        var response = await HttpClient.SendAsync(request);
        return response;
    }

    /// <summary>
    /// The authenticated client sign-in automatically on the first request, but this method
    /// can be used to refresh the token or force a sign in.
    /// </summary>
    public async Task SignInAsync()
    {
        var dto = new SignInDto(Username, Password);
        var response = await HttpClient.PostAsJsonAsync("/api/v1/auth/sign-in", dto);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<SignInResponseDto>();
        _token = content!.Token;
    }
}

public enum BodyType
{
    Json,
    Bytes,
}
