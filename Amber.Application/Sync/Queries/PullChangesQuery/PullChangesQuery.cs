using LiteBus.Queries.Abstractions;

namespace Amber.Application.Sync.Queries.PullChangesQuery;

public record PullChangesQuery(long SinceServerSeq, Guid UserId) : IQuery<PullChangesPageDto>;
