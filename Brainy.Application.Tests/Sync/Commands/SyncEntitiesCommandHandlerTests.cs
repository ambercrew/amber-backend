using AsyncKeyedLock;
using Brainy.Application.Sync.Commands;
using Brainy.Application.Sync.Dto;
using Brainy.Domain.Sync.Configurations;
using Brainy.Domain.Sync.Entities;
using Brainy.Domain.Sync.Repositories;
using Brainy.Domain.Users.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Tests.Sync.Commands;

[TestClass]
public class SyncEntitiesCommandHandlerTests
{
    private SyncEntitiesCommandHandler _handler = null!;
    private ISyncedEntityRepository _syncedEntityRepository = null!;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Username Username = new("test-user");

    [TestInitialize]
    public void Initialize()
    {
        _syncedEntityRepository = Substitute.For<ISyncedEntityRepository>();
        _syncedEntityRepository.CalculateUserUsedStorageInBytesAsync(UserId).Returns(0);
        _syncedEntityRepository
            .CalculateStorageForEntitiesInBytesAsync(UserId, Arg.Any<IList<Guid>>())
            .Returns(0);
    }

    [TestMethod]
    public async Task HandleAsync_NewEntitiesSizeExceedsMax_ThrowsInsufficientStorageException()
    {
        // Arrange

        var dto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        _handler = CreateHandler(maxStoragePerUserInBytes: SizeOf(dto) - 1);

        var command = new SyncEntitiesCommand([dto], UserId, Username);

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InsufficientStorageException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_ExistingStorageExceedsMax_ThrowsInsufficientStorageException()
    {
        // Arrange

        const long existingStorage = 101;
        _handler = CreateHandler(maxStoragePerUserInBytes: existingStorage - 1);
        _syncedEntityRepository
            .CalculateUserUsedStorageInBytesAsync(UserId)
            .Returns(existingStorage);

        var command = new SyncEntitiesCommand([], UserId, Username);

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InsufficientStorageException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_CombinedStorageExceedsMax_ThrowsInsufficientStorageException()
    {
        // Arrange

        const long existingStorage = 20;
        var dto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        _handler = CreateHandler(maxStoragePerUserInBytes: existingStorage + SizeOf(dto) - 1);
        _syncedEntityRepository
            .CalculateUserUsedStorageInBytesAsync(UserId)
            .Returns(existingStorage);

        var command = new SyncEntitiesCommand([dto], UserId, Username);

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InsufficientStorageException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_TotalStorageEqualsMax_UpsertsEntities()
    {
        // Arrange

        var dto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        _handler = CreateHandler(maxStoragePerUserInBytes: SizeOf(dto));

        var command = new SyncEntitiesCommand([dto], UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncedEntityRepository
            .Received(1)
            .UpsertRangeAsync(
                Arg.Is<IList<SyncedEntity>>(entities =>
                    entities.Count == 1 && entities[0].EntityId == dto.EntityId
                )
            );
    }

    [TestMethod]
    public async Task HandleAsync_EmptyDto_UpsertsEmptyList()
    {
        // Arrange

        _handler = CreateHandler(maxStoragePerUserInBytes: 100);

        var command = new SyncEntitiesCommand([], UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncedEntityRepository
            .Received(1)
            .UpsertRangeAsync(Arg.Is<IList<SyncedEntity>>(entities => entities.Count == 0));
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_UpsertsEntities()
    {
        // Arrange

        List<SyncEntityDto> dto =
        [
            new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []),
            new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 2, [1, 2, 3]),
        ];
        _handler = CreateHandler(maxStoragePerUserInBytes: dto.Sum(SizeOf));

        var command = new SyncEntitiesCommand(dto, UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncedEntityRepository
            .Received(1)
            .UpsertRangeAsync(
                Arg.Is<IList<SyncedEntity>>(entities =>
                    entities.Count == 2
                    && entities[0].EntityId == dto[0].EntityId
                    && entities[1].EntityId == dto[1].EntityId
                )
            );
    }

    [TestMethod]
    public async Task HandleAsync_UpsertingExistingEntities_SubtractsOldStorageBeforeChecking()
    {
        // Arrange
        // The entity already exists; its old storage is included in usedStorage.
        // effective total = usedStorage - oldEntitySize + newEntitySize = usedStorage (same size)
        // without subtraction: usedStorage + newEntitySize > usedStorage → would throw

        var dto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        var entitySize = SizeOf(dto);
        var usedStorage = entitySize * 2;

        _handler = CreateHandler(maxStoragePerUserInBytes: usedStorage);
        _syncedEntityRepository.CalculateUserUsedStorageInBytesAsync(UserId).Returns(usedStorage);
        _syncedEntityRepository
            .CalculateStorageForEntitiesInBytesAsync(
                UserId,
                Arg.Is<IList<Guid>>(ids => ids.Contains(dto.EntityId))
            )
            .Returns(entitySize);

        var command = new SyncEntitiesCommand([dto], UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncedEntityRepository
            .Received(1)
            .UpsertRangeAsync(
                Arg.Is<IList<SyncedEntity>>(entities =>
                    entities.Count == 1 && entities[0].EntityId == dto.EntityId
                )
            );
    }

    [TestMethod]
    public async Task HandleAsync_ExistingEntityReplacedWithLargerData_ExceedsMax_ThrowsInsufficientStorageException()
    {
        // Arrange
        // effective total = usedStorage - oldSize + newSize = newSize > max

        var oldDto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        var newDto = new SyncEntityDto(oldDto.EntityId, DateTime.UtcNow, 1, new byte[10]);
        var oldSize = SizeOf(oldDto);
        var newSize = SizeOf(newDto);

        _syncedEntityRepository.CalculateUserUsedStorageInBytesAsync(UserId).Returns(oldSize);
        _syncedEntityRepository
            .CalculateStorageForEntitiesInBytesAsync(
                UserId,
                Arg.Is<IList<Guid>>(ids => ids.Contains(newDto.EntityId))
            )
            .Returns(oldSize);

        _handler = CreateHandler(maxStoragePerUserInBytes: newSize - 1);

        var command = new SyncEntitiesCommand([newDto], UserId, Username);

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InsufficientStorageException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_ExistingEntityReplacedWithLargerData_WithinMax_UpsertsEntities()
    {
        // Arrange
        // effective total = usedStorage - oldSize + newSize = newSize <= max

        var oldDto = new SyncEntityDto(Guid.NewGuid(), DateTime.UtcNow, 1, []);
        var newDto = new SyncEntityDto(oldDto.EntityId, DateTime.UtcNow, 1, new byte[10]);
        var oldSize = SizeOf(oldDto);
        var newSize = SizeOf(newDto);

        _syncedEntityRepository.CalculateUserUsedStorageInBytesAsync(UserId).Returns(oldSize);
        _syncedEntityRepository
            .CalculateStorageForEntitiesInBytesAsync(
                UserId,
                Arg.Is<IList<Guid>>(ids => ids.Contains(newDto.EntityId))
            )
            .Returns(oldSize);

        _handler = CreateHandler(maxStoragePerUserInBytes: newSize);

        var command = new SyncEntitiesCommand([newDto], UserId, Username);

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _syncedEntityRepository
            .Received(1)
            .UpsertRangeAsync(
                Arg.Is<IList<SyncedEntity>>(entities =>
                    entities.Count == 1 && entities[0].EntityId == newDto.EntityId
                )
            );
    }

    private static long SizeOf(SyncEntityDto dto) =>
        new SyncedEntity(
            UserId,
            dto.EntityId,
            dto.CreatedDate,
            DateTime.UtcNow,
            dto.EntityType,
            dto.Data
        ).SizeInBytes;

    private SyncEntitiesCommandHandler CreateHandler(long maxStoragePerUserInBytes) =>
        new(
            _syncedEntityRepository,
            new SyncConfiguration { MaxStoragePerUserInBytes = maxStoragePerUserInBytes },
            new AsyncKeyedLocker<Username>(),
            Substitute.For<ILogger<SyncEntitiesCommandHandler>>()
        );
}
