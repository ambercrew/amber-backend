using Amber.Application.Sync.Dto;

namespace Amber.Application.Sync.Queries.PullChangesQuery;

public record PullChangesPageDto(IList<CellChangeDto> Cells, long NextServerSeq, bool HasMore);
