using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Domain.Sync.ValueObjects;
using Amber.Domain.Users.ValueObjects;
using AsyncKeyedLock;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Sync.Commands;

public class PushChangesCommandHandler(
    ISyncCellRepository syncCellRepository,
    SyncConfiguration syncConfiguration,
    AsyncKeyedLocker<Username> asyncKeyedLocker
) : ICommandHandler<PushChangesCommand>
{
    private static readonly TimeSpan SyncLockTimeOut = TimeSpan.FromMinutes(5);

    public async Task HandleAsync(
        PushChangesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        using var releaser =
            await asyncKeyedLocker.LockOrNullAsync(command.Username, SyncLockTimeOut)
            ?? throw new InternalErrorException("Internal error, try again");

        var cells = command
            .Cells.Select(dto => new SyncCell(
                id: new SyncCellId(command.UserId, dto.Table, dto.RowId, dto.Column),
                value: dto.Value,
                hlc: new Hlc(dto.Hlc),
                deviceId: dto.DeviceId
            ))
            .ToList();

        var applied = await syncCellRepository.TryUpsertCellsAsync(
            command.UserId,
            cells,
            syncConfiguration.MaxStoragePerUserInBytes,
            cancellationToken
        );

        if (!applied)
        {
            throw new InsufficientStorageException(
                "You have exceeded the maximum storage per user."
            );
        }
    }
}
