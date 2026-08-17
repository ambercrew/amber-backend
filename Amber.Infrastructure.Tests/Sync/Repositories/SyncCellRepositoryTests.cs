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
    public async Task TryUpsertCellsAsync_ExceedsMaxStorage_RollsBackAndReturnsFalse()
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

    private SyncCell CreateCell(string rowId, string hlc, string value) =>
        new(
            new SyncCellId(_userId, "notes", rowId, "title"),
            System.Text.Encoding.UTF8.GetBytes(value),
            new Hlc(hlc),
            "device1"
        );
}
