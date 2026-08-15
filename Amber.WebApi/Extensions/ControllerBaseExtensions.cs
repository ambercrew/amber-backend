using System.Security.Claims;
using Amber.Domain.Users.ValueObjects;
using Amber.WebApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Amber.WebApi.Extensions;

public static class ControllerBaseExtensions
{
    public static Username GetSignedInUserUsername(this ControllerBase controllerBase)
    {
        var value = controllerBase
            .HttpContext.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.Name)
            ?.Value;
        if (string.IsNullOrEmpty(value))
            throw new UnauthorizedException("Please sign-in first.");
        return new Username(value);
    }

    public static Guid GetSignedInUserId(this ControllerBase controllerBase)
    {
        var value = controllerBase
            .HttpContext.User?.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)
            ?.Value;
        if (string.IsNullOrEmpty(value))
            throw new UnauthorizedException("Please sign-in first.");
        return Guid.Parse(value);
    }
}
