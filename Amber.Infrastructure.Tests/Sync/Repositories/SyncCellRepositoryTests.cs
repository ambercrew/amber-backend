using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.ValueObjects;
using Amber.Infrastructure.Sync.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;

namespace Amber.Infrastructure.Tests.Sync.Repositories;

[TestClass]
public class SyncCellRepositoryTests : RepositoryTestBase
{
    private SyncCellRepository _syncCellRepository = null!;
    private Guid _userId;

    [TestInitialize]
    public async Task Initialize()
    {
        _syncCellRepository = new(AmberContext);

        var user = UserTestUtils.CreateUser("test-user");
        await AmberContext.Users.AddAsync(user);
        await AmberContext.SaveChangesAsync();
        _userId = user.Id;
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_NewCell_InsertsCellAndAssignsServerSeq()
    {
        // Arrange

        var cell = CreateCell("row-1", "000000000000001-00000001-device1", "value-1");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [cell],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Id.RowId.Should().Be("row-1");
        stored.ServerSeq.Should().Be(1);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_IncomingHlcAfterExisting_OverwritesCellAndAdvancesServerSeq()
    {
        // Arrange

        var older = CreateCell("row-1", "000000000000001-00000001-device1", "old");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [older],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var newer = CreateCell("row-1", "000000000000002-00000001-device1", "new");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [newer],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Value.Should().BeEquivalentTo("new"u8.ToArray());
        stored.ServerSeq.Should().Be(2);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_IncomingHlcBeforeExisting_DiscardsWriteAndKeepsServerSeq()
    {
        // Arrange

        var newer = CreateCell("row-1", "000000000000002-00000001-device1", "new");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [newer],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var older = CreateCell("row-1", "000000000000001-00000001-device1", "old");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [older],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Value.Should().BeEquivalentTo("new"u8.ToArray());
        stored.ServerSeq.Should().Be(1);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_ExceedsMaxStorage_WritesNothingAndReturnsFalse()
    {
        // Arrange

        var cell = CreateCell("row-1", "000000000000001-00000001-device1", "value-1");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [cell],
            maxStoragePerUserInBytes: cell.SizeInBytes - 1
        );

        // Assert

        applied.Should().BeFalse();
        AmberContext.SyncCells.Should().BeEmpty();
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_OverMaxStorageButBatchFreesSpace_AppliesBatch()
    {
        // Arrange

        var body = CreateCell("row-1", "000000000000001-00000001-device1", "a large body");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [body],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [tombstone],
            maxStoragePerUserInBytes: 0
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Id.Column.Should().Be(SyncCell.DeletedColumn);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_OverwriteGrowsPastMaxStorage_KeepsOldCellAndReturnsFalse()
    {
        // Arrange

        var older = CreateCell("row-1", "000000000000001-00000001-device1", "old");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [older],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var newer = CreateCell("row-1", "000000000000002-00000001-device1", "a much longer value");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [newer],
            maxStoragePerUserInBytes: newer.SizeInBytes - 1
        );

        // Assert

        applied.Should().BeFalse();
        var stored = AmberContext.SyncCells.Single();
        stored.Value.Should().BeEquivalentTo("old"u8.ToArray());
    }

    [TestMethod]
    public async Task CalculateUserUsedStorageInBytesAsync_MultipleCells_ReturnsSumOfSizes()
    {
        // Arrange

        var cellA = CreateCell("row-1", "000000000000001-00000001-device1", "aaa");
        var cellB = CreateCell("row-2", "000000000000001-00000001-device1", "bb");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [cellA, cellB],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Act

        var actual = await _syncCellRepository.CalculateUserUsedStorageInBytesAsync(_userId);

        // Assert

        actual.Should().Be(cellA.SizeInBytes + cellB.SizeInBytes);
    }

    [TestMethod]
    public async Task GetCellsAfterServerSeqAsync_ValidInput_ReturnsCellsOrderedByServerSeq()
    {
        // Arrange

        var cellA = CreateCell("row-1", "000000000000001-00000001-device1", "a");
        var cellB = CreateCell("row-2", "000000000000001-00000001-device1", "b");
        var cellC = CreateCell("row-3", "000000000000001-00000001-device1", "c");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [cellA, cellB, cellC],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Act

        var actual = await _syncCellRepository.GetCellsAfterServerSeqAsync(
            _userId,
            sinceServerSeq: 1,
            pageSize: 10
        );

        // Assert

        actual.Should().HaveCount(2);
        actual[0].Id.RowId.Should().Be("row-2");
        actual[1].Id.RowId.Should().Be("row-3");
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_TombstoneNewerThanStoredCells_DeletesThemAndKeepsTombstone()
    {
        // Arrange

        var title = CreateCell("row-1", "000000000000001-00000001-device1", "title");
        var body = CreateCell("row-1", "000000000000001-00000002-device1", "body", "body");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [title, body],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [tombstone],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Id.Column.Should().Be(SyncCell.DeletedColumn);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_CellOlderThanStoredTombstone_DiscardsCell()
    {
        // Arrange

        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [tombstone],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var stale = CreateCell("row-1", "000000000000001-00000001-device1", "stale");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [stale],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.Single();
        stored.Id.Column.Should().Be(SyncCell.DeletedColumn);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_BatchWithTombstone_KeepsOnlyCellsNewerThanIt()
    {
        // Arrange

        var older = CreateCell("row-1", "000000000000001-00000001-device1", "old");
        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");
        var newer = CreateCell("row-1", "000000000000003-00000001-device1", "new", "body");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [older, tombstone, newer],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        AmberContext
            .SyncCells.Select(c => c.Column)
            .Should()
            .BeEquivalentTo([SyncCell.DeletedColumn, "body"]);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_BatchWithTombstoneAndNewerOverwrite_KeepsOverwriteAndDropsOtherCells()
    {
        // Arrange

        var title = CreateCell("row-1", "000000000000001-00000001-device1", "old title");
        var body = CreateCell("row-1", "000000000000001-00000002-device1", "body", "body");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [title, body],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");
        var newerTitle = CreateCell("row-1", "000000000000003-00000001-device1", "new title");

        // Act

        var applied = await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [newerTitle, tombstone],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        applied.Should().BeTrue();
        var stored = AmberContext.SyncCells.OrderBy(c => c.ServerSeq).ToList();
        stored.Select(c => c.Column).Should().Equal(SyncCell.DeletedColumn, "title");
        stored[1].Value.Should().BeEquivalentTo("new title"u8.ToArray());
        stored.Select(c => c.ServerSeq).Should().Equal(3, 4);
    }

    [TestMethod]
    public async Task TryUpsertCellsAsync_TombstoneOlderThanStoredCell_KeepsCell()
    {
        // Arrange

        var newer = CreateCell("row-1", "000000000000003-00000001-device1", "new");
        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [newer],
            maxStoragePerUserInBytes: long.MaxValue
        );

        var tombstone = CreateTombstone("row-1", "000000000000002-00000001-device1");

        // Act

        await _syncCellRepository.TryUpsertCellsAsync(
            _userId,
            [tombstone],
            maxStoragePerUserInBytes: long.MaxValue
        );

        // Assert

        AmberContext
            .SyncCells.Select(c => c.Column)
            .Should()
            .BeEquivalentTo([SyncCell.DeletedColumn, "title"]);
    }

    private SyncCell CreateCell(string rowId, string hlc, string value, string column = "title") =>
        new(
            new SyncCellId(_userId, "notes", rowId, column),
            System.Text.Encoding.UTF8.GetBytes(value),
            new Hlc(hlc),
            "device1"
        );

    private SyncCell CreateTombstone(string rowId, string hlc) =>
        new(
            new SyncCellId(_userId, "notes", rowId, SyncCell.DeletedColumn),
            null,
            new Hlc(hlc),
            "device1"
        );
}
