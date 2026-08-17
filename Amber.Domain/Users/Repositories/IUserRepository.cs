using Amber.Domain.Common.Interfaces;
using Amber.Domain.Users.Entities;
using Amber.Domain.Users.ValueObjects;

namespace Amber.Domain.Users.Repositories;

public interface IUserRepository : IUnitOfWorkRepository
{
    Task<bool> IsEmailUsedAsync(Email email);
    Task<bool> IsUsernameUsedAsync(Username username);
    Task<User?> GetUserByUsernameIfExistsAsync(Username username);
    Task<User> GetUserByUsernameAsync(Username username);
    Task<User?> GetUserByEmailIfExistsAsync(Email email);
    Task<User?> GetUserByGoogleIdIfExistsAsync(string googleId);
    Task AddAsync(User user);
    Task<DateTime> GetUserSignOutDateTimeAsync(Username username);
    Task<User> GetUserByIdAsync(Guid id);

    /// <summary>
    /// This method is not transactional and does not need to call save changes.
    /// Deleting a user also deletes the sync cells associated with it.
    /// </summary>
    Task<int> DeleteUserByIdAsync(Guid id);

    /// <summary>
    /// Returns a list of inactive users where a user is inactive if any of the
    /// following are true:
    /// Users registered for more than 6 months and has no synced cells.
    /// Users with synced cells but the last write to any of them was in 6 months.
    /// </summary>
    Task<IList<Guid>> GetInactiveUsersAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default
    );
}
