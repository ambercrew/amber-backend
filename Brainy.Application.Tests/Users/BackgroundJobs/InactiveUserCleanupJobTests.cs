using Brainy.Application.Users.BackgroundJobs;
using Brainy.Application.Users.Commands.DeleteUser;
using Brainy.Domain.Users.Repositories;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brainy.Application.Tests.Users.BackgroundJobs;

[TestClass]
public class InactiveUserCleanupJobTests
{
    private InactiveUserCleanupJob _job = null!;
    private IUserRepository _userRepository = null!;
    private ICommandMediator _commandMediator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _commandMediator = Substitute.For<ICommandMediator>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUserRepository)).Returns(_userRepository);
        serviceProvider.GetService(typeof(ICommandMediator)).Returns(_commandMediator);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _job = new InactiveUserCleanupJob(
            scopeFactory,
            Substitute.For<ILogger<InactiveUserCleanupJob>>()
        );
    }

    [TestMethod]
    public async Task RunCleanupAsync_NoInactiveUsers_DoesNotSendAnyCommands()
    {
        // Arrange

        _userRepository
            .GetInactiveUsersAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        // Act

        await _job.RunCleanupAsync(CancellationToken.None);

        // Assert

        await _commandMediator
            .DidNotReceive()
            .SendAsync(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task RunCleanupAsync_HasInactiveUsers_SendsDeleteCommandForEachUser()
    {
        // Arrange

        var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        _userRepository
            .GetInactiveUsersAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(userIds);

        // Act

        await _job.RunCleanupAsync(CancellationToken.None);

        // Assert

        foreach (var userId in userIds)
        {
            await _commandMediator
                .Received(1)
                .SendAsync(new DeleteUserCommand(userId), Arg.Any<CancellationToken>());
        }
    }

    [TestMethod]
    public async Task RunCleanupAsync_CommandMediatorThrowsForOneUser_LogsErrorAndContinuesProcessingOtherUsers()
    {
        // Arrange

        var failingUserId = Guid.NewGuid();
        var successUserId = Guid.NewGuid();

        _userRepository
            .GetInactiveUsersAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { failingUserId, successUserId });

        _commandMediator
            .SendAsync(new DeleteUserCommand(failingUserId), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Deletion failed")));

        // Act

        await _job.RunCleanupAsync(CancellationToken.None);

        // Assert

        await _commandMediator
            .Received(1)
            .SendAsync(new DeleteUserCommand(successUserId), Arg.Any<CancellationToken>());
    }
}
