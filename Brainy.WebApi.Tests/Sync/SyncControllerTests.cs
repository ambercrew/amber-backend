using Brainy.Application.Sync.Commands;
using Brainy.Application.Sync.Dto;
using Brainy.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;
using Brainy.Domain.Users.ValueObjects;
using Brainy.WebApi.Sync;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Brainy.WebApi.Tests.Sync;

[TestClass]
public class SyncControllerTests
{
    private MockHttpContext _mockHttpContext = null!;
    private ICommandMediator _commandMediator = null!;
    private IQueryMediator _queryMediator = null!;
    private SyncController _controller = null!;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Username Username = new("test-user");

    [TestInitialize]
    public void Initialize()
    {
        _commandMediator = Substitute.For<ICommandMediator>();
        _queryMediator = Substitute.For<IQueryMediator>();
        _controller = new(_queryMediator, _commandMediator);
        _mockHttpContext = new();
        _mockHttpContext.SetupSignedUser(Username, UserId);
        _controller.ControllerContext.HttpContext = _mockHttpContext;
    }

    [TestMethod]
    public async Task GetSyncedEntitiesAfterOrderedByCreatedDateAsync_ValidInput_ReturnsOkWithResult()
    {
        // Arrange

        var date = DateTime.UtcNow;
        var expectedDto = new SyncedEntitiesPageDto([], false);
        _queryMediator
            .QueryAsync(new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(date, 0, UserId))
            .Returns(expectedDto);

        // Act

        var actual = (OkObjectResult)
            await _controller.GetSyncedEntitiesAfterOrderedByCreatedDateAsync(
                date,
                page: 0,
                CancellationToken.None
            );

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        actual.Value.Should().Be(expectedDto);
    }

    [TestMethod]
    public async Task SyncEntitiesAsync_ValidInput_ReturnsOk()
    {
        // Arrange

        List<SyncEntityDto> dto = [new(Guid.NewGuid(), DateTime.UtcNow, 1, [])];

        // Act

        var actual = (IStatusCodeActionResult)
            await _controller.SyncEntitiesAsync(dto, CancellationToken.None);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task SyncEntitiesAsync_ValidInput_SendsCommandWithCorrectParameters()
    {
        // Arrange

        List<SyncEntityDto> dto = [new(Guid.NewGuid(), DateTime.UtcNow, 1, [])];

        // Act

        await _controller.SyncEntitiesAsync(dto, CancellationToken.None);

        // Assert

        await _commandMediator
            .Received()
            .SendAsync(
                new SyncEntitiesCommand(dto, UserId, Username),
                Arg.Any<CancellationToken>()
            );
    }
}
