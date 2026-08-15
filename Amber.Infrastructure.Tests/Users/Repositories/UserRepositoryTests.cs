using Amber.Domain.Sync.Entities;
using Amber.Domain.Users.Entities;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;

namespace Amber.Infrastructure.Tests.Users.Repositories;

[TestClass]
public class UserRepositoryTests : RepositoryTestBase
{
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new(AmberContext);
    }

    [TestMethod]
    public async Task DeleteUserByIdAsync_UserWithSyncedEntities_DeletedUserAndSyncedEntities()
    {
        // Arrange

        var user1 = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user1);
        await AmberContext.SyncedEntities.AddAsync(CreateSyncedEntity(user1));
        await AmberContext.SyncedEntities.AddAsync(CreateSyncedEntity(user1));

        var user2 = UserTestUtils.CreateUser("user2");
        await _userRepository.AddAsync(user2);
        await AmberContext.SyncedEntities.AddAsync(CreateSyncedEntity(user2));

        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.DeleteUserByIdAsync(user1.Id);

        // Assert

        actual.Should().Be(1);
        // Ensuring that user1 synced entities are deleted and just user2 synced entities exist.
        AmberContext.SyncedEntities.Single().UserId.Should().Be(user2.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_NoUsers_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithNoSyncedEntitiesRegisteredBeforeCutoff_ReturnsUser()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1", registrationDate: cutoffDate.AddDays(-1));
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().ContainSingle().Which.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithNoSyncedEntitiesRegisteredAfterCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1", registrationDate: cutoffDate.AddDays(1));
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithNoSyncedEntitiesRegisteredExactlyAtCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1", registrationDate: cutoffDate);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithSyncedEntityLastSyncedBeforeCutoff_ReturnsUser()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(-1))
        );
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().ContainSingle().Which.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithSyncedEntityLastSyncedAfterCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(1))
        );
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithSyncedEntityLastSyncedExactlyAtCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncedEntities.AddAsync(CreateSyncedEntity(user, cutoffDate));
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithMultipleSyncedEntitiesLatestBeforeCutoff_ReturnsUser()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(-3))
        );
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(-1))
        );
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().ContainSingle().Which.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithMultipleSyncedEntitiesLatestAfterCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(-1))
        );
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(user, cutoffDate.AddDays(1))
        );
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_MixedActiveAndInactiveUsers_ReturnsOnlyInactiveUsers()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;

        var inactiveUserWithSyncedEntity = UserTestUtils.CreateUser("inactive-synced");
        await _userRepository.AddAsync(inactiveUserWithSyncedEntity);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(inactiveUserWithSyncedEntity, cutoffDate.AddDays(-1))
        );

        var inactiveUserWithNoSyncedEntities = UserTestUtils.CreateUser(
            "inactive-no-synced",
            registrationDate: cutoffDate.AddDays(-1)
        );
        await _userRepository.AddAsync(inactiveUserWithNoSyncedEntities);

        var activeUserWithRecentSync = UserTestUtils.CreateUser("active-synced");
        await _userRepository.AddAsync(activeUserWithRecentSync);
        await AmberContext.SyncedEntities.AddAsync(
            CreateSyncedEntity(activeUserWithRecentSync, cutoffDate.AddDays(1))
        );

        var activeUserRecentlyRegistered = UserTestUtils.CreateUser(
            "active-no-synced",
            registrationDate: cutoffDate.AddDays(1)
        );
        await _userRepository.AddAsync(activeUserRecentlyRegistered);

        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().HaveCount(2);
        actual.Should().Contain(inactiveUserWithSyncedEntity.Id);
        actual.Should().Contain(inactiveUserWithNoSyncedEntities.Id);
    }

    private static SyncedEntity CreateSyncedEntity(User user, DateTime? lastSyncDate = null) =>
        new(user.Id, Guid.NewGuid(), DateTime.UtcNow, lastSyncDate ?? DateTime.UtcNow, 1, []);
}
