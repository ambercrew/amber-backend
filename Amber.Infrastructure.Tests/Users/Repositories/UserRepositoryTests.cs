using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.ValueObjects;
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
    public async Task DeleteUserByIdAsync_UserWithSyncCells_DeletedUserAndSyncCells()
    {
        // Arrange

        var user1 = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user1);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user1));
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user1));

        var user2 = UserTestUtils.CreateUser("user2");
        await _userRepository.AddAsync(user2);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user2));

        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.DeleteUserByIdAsync(user1.Id);

        // Assert

        actual.Should().Be(1);
        // Ensuring that user1 sync cells are deleted and just user2 sync cells exist.
        AmberContext.SyncCells.Single().Id.UserId.Should().Be(user2.Id);
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
    public async Task GetInactiveUsersAsync_UserWithNoSyncCellsRegisteredBeforeCutoff_ReturnsUser()
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
    public async Task GetInactiveUsersAsync_UserWithNoSyncCellsRegisteredAfterCutoff_ReturnsEmpty()
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
    public async Task GetInactiveUsersAsync_UserWithNoSyncCellsRegisteredExactlyAtCutoff_ReturnsEmpty()
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
    public async Task GetInactiveUsersAsync_UserWithSyncCellWrittenBeforeCutoff_ReturnsUser()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(-1)));
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().ContainSingle().Which.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithSyncCellWrittenAfterCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(1)));
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithSyncCellWrittenExactlyAtCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate));
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithMultipleSyncCellsLatestBeforeCutoff_ReturnsUser()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(-3)));
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(-1)));
        await _userRepository.SaveChangesAsync();

        // Act

        var actual = await _userRepository.GetInactiveUsersAsync(cutoffDate);

        // Assert

        actual.Should().ContainSingle().Which.Should().Be(user.Id);
    }

    [TestMethod]
    public async Task GetInactiveUsersAsync_UserWithMultipleSyncCellsLatestAfterCutoff_ReturnsEmpty()
    {
        // Arrange

        var cutoffDate = DateTime.UtcNow;
        var user = UserTestUtils.CreateUser("user1");
        await _userRepository.AddAsync(user);
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(-1)));
        await AmberContext.SyncCells.AddAsync(CreateSyncCell(user, cutoffDate.AddDays(1)));
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

        var inactiveUserWithSyncCell = UserTestUtils.CreateUser("inactive-synced");
        await _userRepository.AddAsync(inactiveUserWithSyncCell);
        await AmberContext.SyncCells.AddAsync(
            CreateSyncCell(inactiveUserWithSyncCell, cutoffDate.AddDays(-1))
        );

        var inactiveUserWithNoSyncCells = UserTestUtils.CreateUser(
            "inactive-no-synced",
            registrationDate: cutoffDate.AddDays(-1)
        );
        await _userRepository.AddAsync(inactiveUserWithNoSyncCells);

        var activeUserWithRecentSync = UserTestUtils.CreateUser("active-synced");
        await _userRepository.AddAsync(activeUserWithRecentSync);
        await AmberContext.SyncCells.AddAsync(
            CreateSyncCell(activeUserWithRecentSync, cutoffDate.AddDays(1))
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
        actual.Should().Contain(inactiveUserWithSyncCell.Id);
        actual.Should().Contain(inactiveUserWithNoSyncCells.Id);
    }

    private static SyncCell CreateSyncCell(User user, DateTime? writtenAt = null) =>
        new(
            new SyncCellId(user.Id, "notes", Guid.NewGuid().ToString(), "title"),
            [],
            new Hlc("000000000000001-00000001-device1"),
            "device1"
        )
        {
            ServerSeq = 1,
            WrittenAt = writtenAt ?? DateTime.UtcNow,
        };
}
