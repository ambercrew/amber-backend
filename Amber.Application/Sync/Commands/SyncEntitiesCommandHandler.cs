using AsyncKeyedLock;
using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Amber.Application.Sync.Commands;

public class SyncEntitiesCommandHandler(
    ISyncedEntityRepository syncedEntityRepository,
    SyncConfiguration syncConfiguration,
    AsyncKeyedLocker<Username> asyncKeyedLocker,
    ILogger<SyncEntitiesCommandHandler> logger
) : ICommandHandler<SyncEntitiesCommand>
{
    private static readonly TimeSpan SyncLockTimeOut = TimeSpan.FromMinutes(5);

    public async Task HandleAsync(
        SyncEntitiesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        using var releaser =
            await asyncKeyedLocker.LockOrNullAsync(command.Username, SyncLockTimeOut)
            ?? throw new InternalErrorException("Internal error, try again");

        var syncedEntities = command
            .Dto.Select(val => new SyncedEntity(
                userId: command.UserId,
                entityId: val.EntityId,
                createdDate: val.CreatedDate,
                lastSyncDate: DateTime.UtcNow,
                entityType: val.EntityType,
                data: val.Data
            ))
            .ToList();

        var usedStorage = await syncedEntityRepository.CalculateUserUsedStorageInBytesAsync(
            command.UserId,
            cancellationToken
        );
        logger.LogInformation("Used storage by user in bytes is {UsedStorage}.", usedStorage);

        var entityIds = syncedEntities.Select(e => e.EntityId).ToList();
        var existingEntitiesStorage =
            await syncedEntityRepository.CalculateStorageForEntitiesInBytesAsync(
                command.UserId,
                entityIds,
                cancellationToken
            );
        logger.LogInformation(
            "Storage used in the old existing entities {ExistingEntitiesStorage}",
            existingEntitiesStorage
        );

        var newEntitiesStorage = syncedEntities.Sum(e => e.SizeInBytes);
        logger.LogInformation(
            "Storage in the new entities {NewEntitiesStorage}",
            newEntitiesStorage
        );

        var totalStorage = usedStorage - existingEntitiesStorage + newEntitiesStorage;

        if (totalStorage > syncConfiguration.MaxStoragePerUserInBytes)
        {
            logger.LogWarning(
                "User with id {Id} exceeds the maximum storage, total user storage is {TotalStorage} bytes",
                command.UserId,
                totalStorage
            );
            throw new InsufficientStorageException(
                "You have exceeded the maximum storage per user."
            );
        }

        await syncedEntityRepository.UpsertRangeAsync(syncedEntities, cancellationToken);
    }
}
