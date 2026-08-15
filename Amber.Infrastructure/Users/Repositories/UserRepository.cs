using Amber.Domain.Users.Entities;
using Amber.Domain.Users.Repositories;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Common;
using Amber.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Amber.Infrastructure.Users.Repositories;

public class UserRepository(AmberContext amberContext)
    : UnitOfWorkRepositoryBase(amberContext),
        IUserRepository
{
    public Task<bool> IsEmailUsedAsync(Email email) =>
        AmberContext.Users.AnyAsync(user => user.Email.Value == email.Value);

    public Task<bool> IsUsernameUsedAsync(Username username) =>
        AmberContext.Users.AnyAsync(user => user.Username.Value == username.Value);

    public Task<User?> GetUserByUsernameIfExistsAsync(Username username) =>
        AmberContext.Users.FirstOrDefaultAsync(user => user.Username.Value == username.Value);

    public Task<User> GetUserByUsernameAsync(Username username) =>
        AmberContext.Users.SingleAsync(user => user.Username.Value == username.Value);

    public async Task AddAsync(User user) => await AmberContext.Users.AddAsync(user);

    public Task<DateTime> GetUserSignOutDateTimeAsync(Username username) =>
        AmberContext
            .Users.Where(user => user.Username.Value == username.Value)
            .Select(user => user.SignOutDate)
            .FirstAsync();

    public Task<User> GetUserByIdAsync(Guid id) =>
        AmberContext.Users.SingleAsync(user => user.Id == id);

    public Task<int> DeleteUserByIdAsync(Guid id) =>
        AmberContext.Users.Where(u => u.Id == id).ExecuteDeleteAsync();

    public async Task<IList<Guid>> GetInactiveUsersAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default
    )
    {
        var usersWithSyncedEntities =
            from syncedEntity in AmberContext.SyncedEntities
            group syncedEntity by syncedEntity.UserId into g
            where g.Max(se => se.LastSyncDate) < cutoffDate
            select g.Key;

        var usersWithNoSyncedEntities =
            from user in AmberContext.Users
            where user.RegistrationDate < cutoffDate
            where !AmberContext.SyncedEntities.Any(syncedEntity => syncedEntity.UserId == user.Id)
            select user.Id;

        return await usersWithSyncedEntities
            .Concat(usersWithNoSyncedEntities)
            .ToListAsync(cancellationToken);
    }
}
