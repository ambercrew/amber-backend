using System.Net;
using Amber.WebApi.Sync.Protos;
using Google.Protobuf;

namespace Amber.WebApi.IntegrationTests.Sync.Controllers;

[TestClass]
public class ChangeBatchControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task ReceivesChangeBatchCorrectly()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(
            HttpClient,
            verifyEmail: true
        );
        await authenticatedHttpClient.SignInAsync();

        var changeBatch = new ChangeBatch
        {
            Cells =
            {
                new CellChange
                {
                    Tbl = "todos",
                    RowId = Guid.NewGuid().ToString(),
                    Col = "title",
                    Value = ByteString.CopyFromUtf8("Buy milk"),
                    Hlc = "1755331200000-0000-device1",
                    DeviceId = "device1",
                },
            },
        };

        // Act

        var response = await authenticatedHttpClient.PostAsync(
            "/api/v1/change-batch",
            changeBatch.ToByteArray(),
            bodyType: BodyType.Protobuf
        );

        // Assert

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        var changeBatch = new ChangeBatch();

        // Act

        var response = await authenticatedHttpClient.PostAsync(
            "/api/v1/change-batch",
            changeBatch.ToByteArray(),
            bodyType: BodyType.Protobuf
        );

        // Assert

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
