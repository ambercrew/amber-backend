using Brainy.Domain.Common.Interfaces;
using Brainy.Infrastructure.Database;

namespace Brainy.Infrastructure.Common;

public class UnitOfWorkRepositoryBase(BrainyContext brainyContext) : IUnitOfWorkRepository
{
    protected BrainyContext BrainyContext = brainyContext;

    public async Task SaveChangesAsync()
    {
        await BrainyContext.SaveChangesAsync();
    }
}
