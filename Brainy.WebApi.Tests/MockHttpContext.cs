using System.IO.Pipelines;
using System.Security.Claims;
using Brainy.Domain.Users.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Brainy.WebApi.Tests;

public class MockHttpContext : HttpContext
{
    public readonly IAuthenticationService AuthenticationService;
    private readonly HttpRequest _request;
    private readonly IServiceProvider _serviceProvider;

    public MockHttpContext()
    {
        AuthenticationService = Substitute.For<IAuthenticationService>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _request = Substitute.For<HttpRequest>();
        _serviceProvider.GetService(typeof(IAuthenticationService)).Returns(AuthenticationService);

        var pipeReader = Substitute.For<PipeReader>();
        Request.BodyReader.Returns(pipeReader);
    }

    public void SetupSignedUser(Username? username = null, Guid? id = null)
    {
        IList<Claim> claims = [];

        if (username is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, username.Value));
        }

        if (id is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, id.Value.ToString()));
        }

        ClaimsIdentity claimsIdentity = new(claims, JwtBearerDefaults.AuthenticationScheme);
        ClaimsPrincipal claimsPrincipal = new(claimsIdentity);
        User = claimsPrincipal;
    }

    public override IFeatureCollection Features => throw new NotImplementedException();

    public override HttpRequest Request => _request;

    public override HttpResponse Response => throw new NotImplementedException();

    public override ConnectionInfo Connection => throw new NotImplementedException();

    public override WebSocketManager WebSockets => throw new NotImplementedException();

    public override ClaimsPrincipal User { get; set; } = null!;
    public override IDictionary<object, object?> Items { get; set; } = null!;
    public override IServiceProvider RequestServices
    {
        get => _serviceProvider;
        set => throw new NotImplementedException();
    }
    public override CancellationToken RequestAborted { get; set; }
    public override string TraceIdentifier { get; set; } = null!;
    public override ISession Session { get; set; } = null!;

    public override void Abort() => throw new NotImplementedException();
}
