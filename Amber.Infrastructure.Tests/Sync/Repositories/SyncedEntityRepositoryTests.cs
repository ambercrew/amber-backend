using Amber.Domain.Sync.Entities;
using Amber.Infrastructure.Sync.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;

namespace Amber.Infrastructure.Tests.Sync.Repositories;

[TestClass]
public class SyncedEntityRepositoryTests : RepositoryTestBase
{
    private SyncedEntityRepository _syncedEntityRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _syncedEntityRepository = new(AmberContext);
    }

    [TestMethod]
    public async Task CalculateStorageForEntitiesInBytesAsync_SomeEntityIdsMatch_ReturnsSumOfMatchingEntities()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await AmberContext.Users.AddAsync(user);
        await AmberContext.SaveChangesAsync();

        var matchedEntity1 = CreateSyncedEntity(user.Id, [1, 2, 3]);
        var matchedEntity2 = CreateSyncedEntity(user.Id, [4]);
        var unmatchedEntity = CreateSyncedEntity(user.Id, []);

        await AmberContext.SyncedEntities.AddRangeAsync(
            matchedEntity1,
            matchedEntity2,
            unmatchedEntity
        );
        await AmberContext.SaveChangesAsync();

        var entityIds = new List<Guid> { matchedEntity1.EntityId, matchedEntity2.EntityId };

        // Act

        var actual = await _syncedEntityRepository.CalculateStorageForEntitiesInBytesAsync(
            user.Id,
            entityIds
        );

        // Assert

        actual.Should().Be(matchedEntity1.SizeInBytes + matchedEntity2.SizeInBytes);
    }

    private static SyncedEntity CreateSyncedEntity(Guid userId, byte[] data) =>
        new(userId, Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, 1, data);
}
