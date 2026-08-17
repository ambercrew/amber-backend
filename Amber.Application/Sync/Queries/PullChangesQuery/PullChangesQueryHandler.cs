using Amber.Application.Sync.Dto;
using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Repositories;
using LiteBus.Queries.Abstractions;

namespace Amber.Application.Sync.Queries.PullChangesQuery;

public class PullChangesQueryHandler(
    ISyncCellRepository syncCellRepository,
    SyncConfiguration syncConfiguration
) : IQueryHandler<PullChangesQuery, PullChangesPageDto>
{
    public async Task<PullChangesPageDto> HandleAsync(
        PullChangesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var pageSize = syncConfiguration.SyncCellsPageSize;

        var cells = await syncCellRepository.GetCellsAfterServerSeqAsync(
            query.UserId,
            query.SinceServerSeq,
            pageSize,
            cancellationToken
        );

        var cellDtos = cells
            .Select(c => new CellChangeDto(
                c.Id.Table,
                c.Id.RowId,
                c.Id.Column,
                c.Value,
                c.Hlc.Value,
                c.DeviceId
            ))
            .ToList();

        var nextServerSeq = cells.Count > 0 ? cells[^1].ServerSeq : query.SinceServerSeq;
        var hasMore = cells.Count == pageSize;

        return new PullChangesPageDto(cellDtos, nextServerSeq, hasMore);
    }
}
