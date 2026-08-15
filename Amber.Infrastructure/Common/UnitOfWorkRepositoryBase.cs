using Amber.Domain.Common.Interfaces;
using Amber.Infrastructure.Database;

namespace Amber.Infrastructure.Common;

public class UnitOfWorkRepositoryBase(AmberContext amberContext) : IUnitOfWorkRepository
{
    protected AmberContext AmberContext = amberContext;

    public async Task SaveChangesAsync()
    {
        await AmberContext.SaveChangesAsync();
    }
}
