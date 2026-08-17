using Amber.Application.Sync.Commands;
using Amber.Application.Sync.Dto;
using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Domain.Users.ValueObjects;
using AsyncKeyedLock;

namespace Amber.Application.Tests.Sync.Commands;

[TestClass]
public class PushChangesCommandHandlerTests
{
    private PushChangesCommandHandler _handler = null!;
    private ISyncCellRepository _syncCellRepository = null!;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Username Username = new("test-user");

    [TestMethod]
    public async Task HandleAsync_RepositoryAcceptsBatch_CompletesSuccessfully()
    {
        // Arrange

        _syncCellRepository = Substitute.For<ISyncCellRepository>();
        _syncCellRepository
            .TryUpsertCellsAsync(UserId, Arg.Any<IList<SyncCell>>(), Arg.Any<long>())
            .Returns(true);
        _handler = CreateHandler(maxStoragePerUserInBytes: 100);

        var dto = new CellChangeDto(
            "notes",
            "row-1",
            "title",
            "Buy milk"u8.ToArray(),
            "000000000000001-00000001-device1",
            "device1"
        );
        var command = new PushChangesCommand([dto], UserId, Username);

        // Act & Assert

        await _handler.HandleAsync(command);

        await _syncCellRepository
            .Received(1)
            .TryUpsertCellsAsync(
                UserId,
                Arg.Is<IList<SyncCell>>(cells =>
                    cells.Count == 1
                    && cells[0].Table == dto.Table
                    && cells[0].RowId == dto.RowId
                    && cells[0].Column == dto.Column
                ),
                100
            );
    }

    [TestMethod]
    public async Task HandleAsync_RepositoryRejectsBatch_ThrowsInsufficientStorageException()
    {
        // Arrange

        _syncCellRepository = Substitute.For<ISyncCellRepository>();
        _syncCellRepository
            .TryUpsertCellsAsync(UserId, Arg.Any<IList<SyncCell>>(), Arg.Any<long>())
            .Returns(false);
        _handler = CreateHandler(maxStoragePerUserInBytes: 1);

        var dto = new CellChangeDto(
            "notes",
            "row-1",
            "title",
            "Buy milk"u8.ToArray(),
            "000000000000001-00000001-device1",
            "device1"
        );
        var command = new PushChangesCommand([dto], UserId, Username);

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InsufficientStorageException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_EmptyBatch_UpsertsEmptyList()
    {
        // Arrange

        _syncCellRepository = Substitute.For<ISyncCellRepository>();
        _syncCellRepository
            .TryUpsertCellsAsync(UserId, Arg.Any<IList<SyncCell>>(), Arg.Any<long>())
            .Returns(true);
        _handler = CreateHandler(maxStoragePerUserInBytes: 100);

        var command = new PushChangesCommand([], UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncCellRepository
            .Received(1)
            .TryUpsertCellsAsync(UserId, Arg.Is<IList<SyncCell>>(cells => cells.Count == 0), 100);
    }

    private PushChangesCommandHandler CreateHandler(long maxStoragePerUserInBytes) =>
        new(
            _syncCellRepository,
            new SyncConfiguration { MaxStoragePerUserInBytes = maxStoragePerUserInBytes },
            new AsyncKeyedLocker<Username>()
        );
}
