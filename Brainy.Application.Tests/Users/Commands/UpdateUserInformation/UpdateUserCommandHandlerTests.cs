using Brainy.Application.Users.Commands.UpdateUserInformation;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Users.Repositories;
using Brainy.TestUtils;
using Brainy.TestUtils.Users;

namespace Brainy.Application.Tests.Users.Commands.UpdateUserInformation;

[TestClass]
public class UpdateUserCommandHandlerTests : RepositoryTestBase
{
    private UpdateUserCommandHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _userRepository = new UserRepository(BrainyContext);
        _handler = new UpdateUserCommandHandler(_userRepository);

        var user = UserTestUtils.CreateUser(
            "test-user",
            firstName: "Old first name",
            lastName: "Old last name"
        );
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
    }

    [TestMethod]
    public async Task HandleAsync_OnlyFirstNameProvided_UpdatesOnlyFirstName()
    {
        // Arrange

        var command = new UpdateUserCommand(
            new UpdateUserInformationDto(FirstName: "New first name", LastName: null),
            new Username("test-user")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var user = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        user.FirstName.Should().Be("New first name");
        user.LastName.Should().Be("Old last name");
    }

    [TestMethod]
    public async Task HandleAsync_OnlyLastNameProvided_UpdatesOnlyLastName()
    {
        // Arrange

        var command = new UpdateUserCommand(
            new UpdateUserInformationDto(FirstName: null, LastName: "New last name"),
            new Username("test-user")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var user = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        user.FirstName.Should().Be("Old first name");
        user.LastName.Should().Be("New last name");
    }

    [TestMethod]
    public async Task HandleAsync_BothFieldsEmpty_UpdatesNothing()
    {
        // Arrange

        var command = new UpdateUserCommand(
            new UpdateUserInformationDto(FirstName: null, LastName: null),
            new Username("test-user")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var user = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        user.FirstName.Should().Be("Old first name");
        user.LastName.Should().Be("Old last name");
    }

    [TestMethod]
    public async Task HandleAsync_BothFieldsProvided_UpdatesBothFields()
    {
        // Arrange

        var command = new UpdateUserCommand(
            new UpdateUserInformationDto(FirstName: "New first name", LastName: "New last name"),
            new Username("test-user")
        );

        // Act

        await _handler.HandleAsync(command);

        // Assert

        var user = await _userRepository.GetUserByUsernameAsync(new Username("test-user"));
        user.FirstName.Should().Be("New first name");
        user.LastName.Should().Be("New last name");
    }
}
