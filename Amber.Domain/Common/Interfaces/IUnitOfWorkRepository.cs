namespace Amber.Domain.Common.Interfaces;

/// <summary>
/// A type of repository that requires call to save methods for changes to be saved.
/// </summary>
public interface IUnitOfWorkRepository : IRepository
{
    Task SaveChangesAsync();
}
