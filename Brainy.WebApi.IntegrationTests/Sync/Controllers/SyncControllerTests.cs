using System.Net;
using System.Net.Http.Json;
using Brainy.Application.Sync.Dto;
using Brainy.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;

namespace Brainy.WebApi.IntegrationTests.Sync.Controllers;

[TestClass]
public class SyncControllerTests : IntegrationTestBase
{
    [TestMethod]
    public async Task SyncsEntitiesCorrectly()
    {
        // Arrange

        var authenticatedHttpClient = await AuthenticatedHttpClient.CreateAsync(
            HttpClient,
            verifyEmail: true
        );
        await authenticatedHttpClient.SignInAsync();

        var date = DateTime.UtcNow;
        List<SyncEntityDto> excludedEntities =
        [
            new SyncEntityDto(Guid.NewGuid(), date, 1, []),
            new SyncEntityDto(Guid.NewGuid(), date - TimeSpan.FromMinutes(1), 1, [1]),
        ];

        var firstSendResponse = await authenticatedHttpClient.PostAsync(
            "/api/v1/sync",
            excludedEntities
        );
        firstSendResponse.EnsureSuccessStatusCode();
        var firstSendDateTime = DateTime.UtcNow;

        List<SyncEntityDto> includedEntities =
        [
            new SyncEntityDto(Guid.NewGuid(), date - TimeSpan.FromDays(4), 2, []),
            new SyncEntityDto(Guid.NewGuid(), date - TimeSpan.FromDays(7), 2, [1]),
            new SyncEntityDto(Guid.NewGuid(), date - TimeSpan.FromDays(2), 2, [12]),
        ];

        var secondSendResponse = await authenticatedHttpClient.PostAsync(
            "/api/v1/sync",
            includedEntities
        );
        secondSendResponse.EnsureSuccessStatusCode();

        // Act

        var result = await authenticatedHttpClient.GetAsync(
            $"/api/v1/sync?date={firstSendDateTime:o}&page=0"
        );

        // Assert

        result.EnsureSuccessStatusCode();
        var actual = (await result.Content.ReadFromJsonAsync<SyncedEntitiesPageDto>())!;
        actual.HasMore.Should().BeFalse();
        actual.SyncedEntities.Should().HaveCount(3);
        actual.SyncedEntities[0].EntityId.Should().Be(includedEntities[1].EntityId);
        actual.SyncedEntities[1].EntityId.Should().Be(includedEntities[0].EntityId);
        actual.SyncedEntities[2].EntityId.Should().Be(includedEntities[2].EntityId);
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
            "/api/v1/sync",
            new List<SyncEntityDto>()
        );

        // Assert

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
