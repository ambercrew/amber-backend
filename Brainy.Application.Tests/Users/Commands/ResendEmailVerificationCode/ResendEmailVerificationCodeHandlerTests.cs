using AsyncKeyedLock;
using Brainy.Application.Users.Commands.ResendEmailVerificationCode;
using Brainy.Application.Users.Services;
using Brainy.Domain.Users.Entities;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Users.Repositories;
using Brainy.TestUtils;
using Brainy.TestUtils.Users;

namespace Brainy.Application.Tests.Users.Commands.ResendEmailVerificationCode;

[TestClass]
public class ResendEmailVerificationCodeCommandHandlerTests : RepositoryTestBase
{
    private ResendEmailVerificationCodeCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;
    private IUserEmailService _userEmailService = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(BrainyContext);
        _userEmailService = Substitute.For<IUserEmailService>();
        _handler = new ResendEmailVerificationCodeCommandHandler(
            _userEmailService,
            _userRepository,
            new AsyncKeyedLocker<Username>()
        );
    }

    [TestMethod]
    public async Task HandleAsync_SentTooRecently_ThrowsInvalidOperationException()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        user.LastDateTimeOfSentVerificationCode = DateTime.UtcNow;
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new ResendEmailVerificationCodeCommand(new Username("test-user"));

        // Act & Assert

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await _handler.HandleAsync(command)
        );
    }

    [TestMethod]
    public async Task HandleAsync_AfterMinimumTime_SendsVerificationEmail()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        user.LastDateTimeOfSentVerificationCode =
            DateTime.UtcNow
            - User.MinimumTimeBetweenResendingEmailVerificationCode
            - TimeSpan.FromSeconds(1);
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var command = new ResendEmailVerificationCodeCommand(new Username("test-user"));

        // Act

        await _handler.HandleAsync(command);

        // Assert

        await _userEmailService.Received(1).SendVerificationEmailAsync(Arg.Any<User>());
    }
}
