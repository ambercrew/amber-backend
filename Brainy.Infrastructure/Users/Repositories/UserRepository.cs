using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.Repositories;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Common;
using Brainy.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Brainy.Infrastructure.Users.Repositories;

public class UserRepository(BrainyContext brainyContext)
    : UnitOfWorkRepositoryBase(brainyContext),
        IUserRepository
{
    public Task<bool> IsEmailUsedAsync(Email email) =>
        BrainyContext.Users.AnyAsync(user => user.Email.Value == email.Value);

    public Task<bool> IsUsernameUsedAsync(Username username) =>
        BrainyContext.Users.AnyAsync(user => user.Username.Value == username.Value);

    public Task<User?> GetUserByUsernameIfExistsAsync(Username username) =>
        BrainyContext.Users.FirstOrDefaultAsync(user => user.Username.Value == username.Value);

    public Task<User> GetUserByUsernameAsync(Username username) =>
        BrainyContext.Users.SingleAsync(user => user.Username.Value == username.Value);

    public async Task AddAsync(User user) => await BrainyContext.Users.AddAsync(user);

    public Task<DateTime> GetUserSignOutDateTimeAsync(Username username) =>
        BrainyContext
            .Users.Where(user => user.Username.Value == username.Value)
            .Select(user => user.SignOutDate)
            .FirstAsync();

    public Task<User> GetUserByIdAsync(Guid id) =>
        BrainyContext.Users.SingleAsync(user => user.Id == id);

    public Task<int> DeleteUserByIdAsync(Guid id) =>
        BrainyContext.Users.Where(u => u.Id == id).ExecuteDeleteAsync();

    public async Task<IList<Guid>> GetInactiveUsersAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default
    )
    {
        var usersWithSyncedEntities =
            from syncedEntity in BrainyContext.SyncedEntities
            group syncedEntity by syncedEntity.UserId into g
            where g.Max(se => se.LastSyncDate) < cutoffDate
            select g.Key;

        var usersWithNoSyncedEntities =
            from user in BrainyContext.Users
            where user.RegistrationDate < cutoffDate
            where !BrainyContext.SyncedEntities.Any(syncedEntity => syncedEntity.UserId == user.Id)
            select user.Id;

        return await usersWithSyncedEntities
            .Concat(usersWithNoSyncedEntities)
            .ToListAsync(cancellationToken);
    }
}
