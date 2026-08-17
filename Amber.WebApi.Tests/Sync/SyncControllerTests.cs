using Amber.Application.Sync.Commands;
using Amber.Application.Sync.Dto;
using Amber.Application.Sync.Queries.PullChangesQuery;
using Amber.Domain.Users.ValueObjects;
using Amber.WebApi.Sync;
using Amber.WebApi.Sync.Protos;
using Google.Protobuf;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Amber.WebApi.Tests.Sync;

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
    public async Task PushAsync_ValidInput_ReturnsOk()
    {
        // Arrange

        SetRequestBody(BuildChangeBatch());

        // Act

        var actual = (IStatusCodeActionResult)await _controller.PushAsync(CancellationToken.None);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task PushAsync_ValidInput_SendsCommandWithCorrectParameters()
    {
        // Arrange

        SetRequestBody(BuildChangeBatch());

        // Act

        await _controller.PushAsync(CancellationToken.None);

        // Assert

        await _commandMediator
            .Received()
            .SendAsync(
                Arg.Is<PushChangesCommand>(command =>
                    command.UserId == UserId
                    && command.Username == Username
                    && command.Cells.Count == 1
                    && command.Cells[0].Table == "notes"
                    && command.Cells[0].RowId == "row-1"
                    && command.Cells[0].Column == "title"
                    && command.Cells[0].Hlc == "000000000000001-00000001-device1"
                    && command.Cells[0].DeviceId == "device1"
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [TestMethod]
    public async Task PullAsync_ValidInput_ReturnsOkWithSerializedResult()
    {
        // Arrange

        var expectedDto = new PullChangesPageDto(
            [new CellChangeDto("notes", "row-1", "title", "Hello"u8.ToArray(), "hlc-1", "device1")],
            NextServerSeq: 1,
            HasMore: false
        );
        _queryMediator.QueryAsync(new PullChangesQuery(0, UserId)).Returns(expectedDto);

        // Act

        var actual = (FileContentResult)
            await _controller.PullAsync(sinceServerSeq: 0, CancellationToken.None);

        // Assert

        actual.ContentType.Should().Be("application/x-protobuf");
        var response = PullResponse.Parser.ParseFrom(actual.FileContents);
        response.NextServerSeq.Should().Be(1);
        response.HasMore.Should().BeFalse();
        response.Cells.Should().HaveCount(1);
        response.Cells[0].Tbl.Should().Be("notes");
        response.Cells[0].RowId.Should().Be("row-1");
        response.Cells[0].Col.Should().Be("title");
    }

    [TestMethod]
    public async Task PullAsync_CellHasNoValue_ReturnsCellWithUnsetValue()
    {
        // Arrange

        var expectedDto = new PullChangesPageDto(
            [new CellChangeDto("notes", "row-1", "__deleted", null, "hlc-1", "device1")],
            NextServerSeq: 1,
            HasMore: false
        );
        _queryMediator.QueryAsync(new PullChangesQuery(0, UserId)).Returns(expectedDto);

        // Act

        var actual = (FileContentResult)
            await _controller.PullAsync(sinceServerSeq: 0, CancellationToken.None);

        // Assert

        var response = PullResponse.Parser.ParseFrom(actual.FileContents);
        response.Cells.Should().HaveCount(1);
        response.Cells[0].Col.Should().Be("__deleted");
        response.Cells[0].HasValue.Should().BeFalse();
    }

    private void SetRequestBody(ChangeBatch batch)
    {
        var stream = new MemoryStream(batch.ToByteArray());
        _mockHttpContext.Request.Body.Returns(stream);
    }

    private static ChangeBatch BuildChangeBatch() =>
        new()
        {
            Cells =
            {
                new CellChange
                {
                    Tbl = "notes",
                    RowId = "row-1",
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("Buy milk"),
                    Hlc = "000000000000001-00000001-device1",
                    DeviceId = "device1",
                },
            },
        };
}
