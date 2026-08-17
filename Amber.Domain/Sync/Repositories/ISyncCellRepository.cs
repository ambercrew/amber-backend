using Amber.Domain.Common.Interfaces;
using Amber.Domain.Sync.Entities;

namespace Amber.Domain.Sync.Repositories;

public interface ISyncCellRepository : IUnitOfWorkRepository
{
    /// <summary>
    /// Applies the given cells for the user using last-write-wins conflict resolution:
    /// a cell is only written (inserted or updated) if it has no existing counterpart, or
    /// its HLC is after the existing cell's HLC. Winning cells are assigned a new
    /// <see cref="SyncCell.ServerSeq"/>. If applying the batch would push the user's used
    /// storage above <paramref name="maxStoragePerUserInBytes"/>, nothing is persisted.
    /// </summary>
    /// <returns>true if the batch was applied; false if it was rejected for exceeding the storage limit.</returns>
    Task<bool> TryUpsertCellsAsync(
        Guid userId,
        IList<SyncCell> cells,
        long maxStoragePerUserInBytes,
        CancellationToken cancellationToken = default
    );

    Task<IList<SyncCell>> GetCellsAfterServerSeqAsync(
        Guid userId,
        long sinceServerSeq,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<long> CalculateUserUsedStorageInBytesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
