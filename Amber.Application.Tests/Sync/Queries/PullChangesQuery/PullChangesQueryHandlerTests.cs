using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Entities;
using Amber.Domain.Sync.Repositories;
using Amber.Domain.Sync.ValueObjects;

namespace Amber.Application.Tests.Sync.Queries.PullChangesQuery;

[TestClass]
public class PullChangesQueryHandlerTests
{
    private Application.Sync.Queries.PullChangesQuery.PullChangesQueryHandler _handler = null!;
    private ISyncCellRepository _syncCellRepository = null!;

    private static readonly Guid UserId = Guid.NewGuid();

    [TestInitialize]
    public void Initialize()
    {
        _syncCellRepository = Substitute.For<ISyncCellRepository>();
        _handler = new(_syncCellRepository, new SyncConfiguration { SyncCellsPageSize = 2 });
    }

    [TestMethod]
    public async Task HandleAsync_FullPage_ReturnsHasMoreTrueAndLastCellsServerSeq()
    {
        // Arrange

        List<SyncCell> cells = [CreateCell(1), CreateCell(2)];
        _syncCellRepository.GetCellsAfterServerSeqAsync(UserId, 0, 2).Returns(cells);

        var query = new Application.Sync.Queries.PullChangesQuery.PullChangesQuery(0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Cells.Should().HaveCount(2);
        result.HasMore.Should().BeTrue();
        result.NextServerSeq.Should().Be(2);
    }

    [TestMethod]
    public async Task HandleAsync_LessThanFullPage_ReturnsHasMoreFalse()
    {
        // Arrange

        List<SyncCell> cells = [CreateCell(1)];
        _syncCellRepository.GetCellsAfterServerSeqAsync(UserId, 0, 2).Returns(cells);

        var query = new Application.Sync.Queries.PullChangesQuery.PullChangesQuery(0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Cells.Should().HaveCount(1);
        result.HasMore.Should().BeFalse();
        result.NextServerSeq.Should().Be(1);
    }

    [TestMethod]
    public async Task HandleAsync_NoCells_ReturnsSinceServerSeqAsNextServerSeq()
    {
        // Arrange

        _syncCellRepository.GetCellsAfterServerSeqAsync(UserId, 5, 2).Returns([]);

        var query = new Application.Sync.Queries.PullChangesQuery.PullChangesQuery(5, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Cells.Should().BeEmpty();
        result.HasMore.Should().BeFalse();
        result.NextServerSeq.Should().Be(5);
    }

    private static SyncCell CreateCell(long serverSeq) =>
        new(
            new SyncCellId(UserId, "notes", Guid.NewGuid().ToString(), "title"),
            [],
            new Hlc("000000000000001-00000001-device1"),
            "device1"
        )
        {
            ServerSeq = serverSeq,
        };
}
