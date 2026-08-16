using System.Net;
using Amber.WebApi.Sync.Protos;
using Google.Protobuf;

namespace Amber.WebApi.IntegrationTests.Sync.Controllers;

[TestClass]
public class SyncControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task PushThenPull_ValidInput_ReturnsPushedCells()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(
            HttpClient,
            verifyEmail: true
        );
        await authenticatedHttpClient.SignInAsync();

        var batch = new ChangeBatch
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
                new CellChange
                {
                    Tbl = "notes",
                    RowId = "row-2",
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("Walk the dog"),
                    Hlc = "000000000000002-00000001-device1",
                    DeviceId = "device1",
                },
            },
        };

        var pushResponse = await authenticatedHttpClient.PostAsync(
            "/api/v1/sync/push",
            batch.ToByteArray(),
            bodyType: BodyType.Protobuf
        );
        pushResponse.EnsureSuccessStatusCode();

        // Act

        var pullResponse = await authenticatedHttpClient.GetAsync(
            "/api/v1/sync/pull?sinceServerSeq=0"
        );

        // Assert

        pullResponse.EnsureSuccessStatusCode();
        var result = PullResponse.Parser.ParseFrom(
            await pullResponse.Content.ReadAsByteArrayAsync()
        );
        result.HasMore.Should().BeFalse();
        result.Cells.Should().HaveCount(2);
        result.Cells[0].RowId.Should().Be("row-1");
        result.Cells[1].RowId.Should().Be("row-2");
    }

    [TestMethod]
    public async Task Pull_OlderWriteAfterNewerWrite_KeepsNewerWrite()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(
            HttpClient,
            verifyEmail: true
        );
        await authenticatedHttpClient.SignInAsync();

        var newerWrite = new ChangeBatch
        {
            Cells =
            {
                new CellChange
                {
                    Tbl = "notes",
                    RowId = "row-1",
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("newer"),
                    Hlc = "000000000000002-00000001-device1",
                    DeviceId = "device1",
                },
            },
        };
        var olderWrite = new ChangeBatch
        {
            Cells =
            {
                new CellChange
                {
                    Tbl = "notes",
                    RowId = "row-1",
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("older"),
                    Hlc = "000000000000001-00000001-device1",
                    DeviceId = "device1",
                },
            },
        };

        (
            await authenticatedHttpClient.PostAsync(
                "/api/v1/sync/push",
                newerWrite.ToByteArray(),
                bodyType: BodyType.Protobuf
            )
        ).EnsureSuccessStatusCode();
        (
            await authenticatedHttpClient.PostAsync(
                "/api/v1/sync/push",
                olderWrite.ToByteArray(),
                bodyType: BodyType.Protobuf
            )
        ).EnsureSuccessStatusCode();

        // Act

        var pullResponse = await authenticatedHttpClient.GetAsync(
            "/api/v1/sync/pull?sinceServerSeq=0"
        );

        // Assert

        var result = PullResponse.Parser.ParseFrom(
            await pullResponse.Content.ReadAsByteArrayAsync()
        );
        result.Cells.Should().ContainSingle();
        result.Cells[0].Value.ToStringUtf8().Should().Be("newer");
    }

    [TestMethod]
    public async Task Pull_AnotherUsersCells_ReturnsOnlyOwnCells()
    {
        // Arrange

        var owner = await AuthenticatedHttpClient.CreateAsync(HttpClient, verifyEmail: true);
        await owner.SignInAsync();

        var otherUser = await AuthenticatedHttpClient.CreateAsync(HttpClient, verifyEmail: true);
        await otherUser.SignInAsync();

        var batch = new ChangeBatch
        {
            Cells =
            {
                new CellChange
                {
                    Tbl = "notes",
                    RowId = "row-1",
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("owner's note"),
                    Hlc = "000000000000001-00000001-device1",
                    DeviceId = "device1",
                },
            },
        };

        (
            await owner.PostAsync(
                "/api/v1/sync/push",
                batch.ToByteArray(),
                bodyType: BodyType.Protobuf
            )
        ).EnsureSuccessStatusCode();

        // Act

        var pullResponse = await otherUser.GetAsync("/api/v1/sync/pull?sinceServerSeq=0");

        // Assert

        var result = PullResponse.Parser.ParseFrom(
            await pullResponse.Content.ReadAsByteArrayAsync()
        );
        result.Cells.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReturnsForbiddenForUnverifiedUser()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(
            HttpClient,
            verifyEmail: false
        );
        await authenticatedHttpClient.SignInAsync();

        // Act

        var response = await authenticatedHttpClient.PostAsync(
            "/api/v1/sync/push",
            new ChangeBatch().ToByteArray(),
            bodyType: BodyType.Protobuf
        );

        // Assert

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
