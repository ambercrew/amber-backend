using Brainy.Domain.Common.Interfaces;
using Brainy.Domain.Sync.Entities;
using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.ValueObjects;

namespace Brainy.Domain.Users.Repositories;

public interface IUserRepository : IUnitOfWorkRepository
{
    Task<bool> IsEmailUsedAsync(Email email);
    Task<bool> IsUsernameUsedAsync(Username username);
    Task<User?> GetUserByUsernameIfExistsAsync(Username username);
    Task<User> GetUserByUsernameAsync(Username username);
    Task AddAsync(User user);
    Task<DateTime> GetUserSignOutDateTimeAsync(Username username);
    Task<User> GetUserByIdAsync(Guid id);

    /// <summary>
    /// This method is not transactional and does not need to call save changes.
    /// Deleting a user also deletes the synced entities associated with it.
    /// </summary>
    Task<int> DeleteUserByIdAsync(Guid id);

    /// <summary>
    /// Returns a list of inactive users where a user is inactive if any of the
    /// following are true:
    /// Users registered for more than 6 months and has no <see cref="SyncedEntity" />.
    /// Users with <see cref="SyncedEntity" /> but the last sync was in 6 months.
    /// </summary>
    Task<IList<Guid>> GetInactiveUsersAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default
    );
}
