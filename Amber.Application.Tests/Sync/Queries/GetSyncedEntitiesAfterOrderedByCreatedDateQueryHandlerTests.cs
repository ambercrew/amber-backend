using Amber.Application.Sync.Queries.GetSyncedEntitiesAfterOrderedByCreatedDateQuery;
using Amber.Domain.Sync.Configurations;
using Amber.Domain.Sync.Entities;
using Amber.Infrastructure.Sync.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;

namespace Amber.Application.Tests.Sync.Queries;

[TestClass]
public class GetSyncedEntitiesAfterOrderedByCreatedDateQueryHandlerTests : RepositoryTestBase
{
    private GetSyncedEntitiesAfterOrderedByCreatedDateQueryHandler _handler = null!;
    private SyncedEntityRepository _syncedEntityRepository = null!;

    private static readonly Guid UserId = Guid.NewGuid();
    private readonly DateTime _date = DateTime.UtcNow;

    [TestInitialize]
    public async Task Initialize()
    {
        _syncedEntityRepository = new SyncedEntityRepository(AmberContext);

        await AmberContext.Users.AddAsync(UserTestUtils.CreateUser("test-user", id: UserId));
        await AmberContext.SaveChangesAsync();
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_ReturnsEntitiesWithLastSyncDateAfterDate()
    {
        // Arrange

        _handler = CreateHandler(pageSize: 10);

        List<SyncedEntity> includedEntities =
        [
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(2),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            ),
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(1),
                lastSyncDate: _date + TimeSpan.FromSeconds(1)
            ),
        ];
        List<SyncedEntity> excludedEntities =
        [
            CreateEntity(createdDate: _date, lastSyncDate: _date),
            CreateEntity(createdDate: _date, lastSyncDate: _date - TimeSpan.FromMinutes(1)),
        ];
        await AmberContext.SyncedEntities.AddRangeAsync([.. includedEntities, .. excludedEntities]);
        await AmberContext.SaveChangesAsync();

        var query = new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(_date, Page: 0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.SyncedEntities.Should().HaveCount(2);
        result
            .SyncedEntities.Select(e => e.EntityId)
            .Should()
            .BeEquivalentTo(includedEntities.Select(e => e.EntityId));
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_ReturnsEntitiesOrderedByCreatedDate()
    {
        // Arrange

        _handler = CreateHandler(pageSize: 10);

        var entityA = CreateEntity(
            createdDate: _date - TimeSpan.FromDays(4),
            lastSyncDate: _date + TimeSpan.FromMinutes(1)
        );
        var entityB = CreateEntity(
            createdDate: _date - TimeSpan.FromDays(7),
            lastSyncDate: _date + TimeSpan.FromMinutes(1)
        );
        var entityC = CreateEntity(
            createdDate: _date - TimeSpan.FromDays(2),
            lastSyncDate: _date + TimeSpan.FromSeconds(1)
        );

        await AmberContext.SyncedEntities.AddRangeAsync(entityA, entityB, entityC);
        await AmberContext.SaveChangesAsync();

        var query = new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(_date, Page: 0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.SyncedEntities.Should().HaveCount(3);
        result.SyncedEntities[0].EntityId.Should().Be(entityB.EntityId); // oldest
        result.SyncedEntities[1].EntityId.Should().Be(entityA.EntityId);
        result.SyncedEntities[2].EntityId.Should().Be(entityC.EntityId); // newest
    }

    [TestMethod]
    public async Task HandleAsync_FullPage_ReturnsHasMoreTrue()
    {
        // Arrange

        _handler = CreateHandler(pageSize: 2);

        await AmberContext.SyncedEntities.AddRangeAsync(
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(3),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            ),
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(2),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            ),
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(1),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            )
        );
        await AmberContext.SaveChangesAsync();

        var query = new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(_date, Page: 0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.SyncedEntities.Should().HaveCount(2);
        result.HasMore.Should().BeTrue();
    }

    [TestMethod]
    public async Task HandleAsync_LessThanFullPage_ReturnsHasMoreFalse()
    {
        // Arrange

        _handler = CreateHandler(pageSize: 5);

        await AmberContext.SyncedEntities.AddRangeAsync(
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(2),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            ),
            CreateEntity(
                createdDate: _date - TimeSpan.FromDays(1),
                lastSyncDate: _date + TimeSpan.FromMinutes(1)
            )
        );
        await AmberContext.SaveChangesAsync();

        var query = new GetSyncedEntitiesAfterOrderedByCreatedDateQuery(_date, Page: 0, UserId);

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.SyncedEntities.Should().HaveCount(2);
        result.HasMore.Should().BeFalse();
    }

    private SyncedEntity CreateEntity(DateTime createdDate, DateTime lastSyncDate) =>
        new(UserId, Guid.NewGuid(), createdDate, lastSyncDate, entityType: 1, data: []);

    private GetSyncedEntitiesAfterOrderedByCreatedDateQueryHandler CreateHandler(int pageSize) =>
        new(_syncedEntityRepository, new SyncConfiguration { SyncedEntitiesPageSize = pageSize });
}
