using Amber.WebApi.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Amber.WebApi.Tests.Middlewares;

[TestClass]
public class DefaultExceptionHandlerTests
{
    [TestMethod]
    public async Task TryHandleAsync_RequestBodyTooLarge_Returns413()
    {
        // Arrange

        var handler = new DefaultExceptionHandler<BadHttpRequestException>(
            Substitute.For<ILogger<BadHttpRequestException>>()
        );
        var httpContext = new DefaultHttpContext();
        var exception = new BadHttpRequestException(
            "Request body too large.",
            StatusCodes.Status413PayloadTooLarge
        );

        // Act

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert

        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [TestMethod]
    public async Task TryHandleAsync_OtherExceptionType_ReturnsFalse()
    {
        // Arrange

        var handler = new DefaultExceptionHandler<BadHttpRequestException>(
            Substitute.For<ILogger<BadHttpRequestException>>()
        );

        // Act

        var actual = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException(),
            CancellationToken.None
        );

        // Assert

        actual.Should().BeFalse();
    }
}
