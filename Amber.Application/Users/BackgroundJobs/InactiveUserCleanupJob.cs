using System.Runtime.CompilerServices;
using Amber.Application.Users.Commands.DeleteUser;
using Amber.Domain.Users.Repositories;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Amber.Application.Tests")]

namespace Amber.Application.Users.BackgroundJobs;

public class InactiveUserCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<InactiveUserCleanupJob> logger
) : BackgroundService
{
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromDays(182); // ~ 6 Months.
    private static readonly TimeSpan DelayBetweenWaits = TimeSpan.FromHours(1);

    /// <summary>
    /// Describes at which hour the job should run.
    /// </summary>
    private const int DailyRuntimeHour = 2;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.UtcNow.Hour == DailyRuntimeHour)
            {
                await RunCleanupAsync(stoppingToken);
            }

            await Task.Delay(DelayBetweenWaits, stoppingToken);
        }
    }

    internal async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting inactive user cleanup job.");

            var cutoffDate = DateTime.UtcNow - InactivityThreshold;

            await using var scope = scopeFactory.CreateAsyncScope();

            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var inactiveUsers = await userRepository.GetInactiveUsersAsync(
                cutoffDate,
                cancellationToken
            );

            logger.LogInformation("Found {Count} inactive user(s) to delete.", inactiveUsers.Count);

            var commandMediator = scope.ServiceProvider.GetRequiredService<ICommandMediator>();

            foreach (var userId in inactiveUsers)
            {
                try
                {
                    await commandMediator.SendAsync(
                        new DeleteUserCommand(userId),
                        cancellationToken
                    );
                    logger.LogInformation("Deleted inactive user {UserId}.", userId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process inactive user {UserId}.", userId);
                }
            }

            logger.LogInformation("Inactive user cleanup job completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured during daily deletion of inactive users");
        }
    }
}
