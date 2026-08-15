using Amber.Application.Users.Queries.GetUserByUsername;
using Amber.Domain.Users.ValueObjects;
using Amber.Infrastructure.Users.Repositories;
using Amber.TestUtils;
using Amber.TestUtils.Users;

namespace Amber.Application.Tests.Users.Queries.GetUserByUsername;

[TestClass]
public class GetUserByUsernameQueryHandlerTests : RepositoryTestBase
{
    private GetUserByUsernameQueryHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(AmberContext);
        _handler = new GetUserByUsernameQueryHandler(_userRepository);
    }

    [TestMethod]
    public async Task HandleAsync_ValidInput_ReturnsUserInformationDto()
    {
        // Arrange

        var user = UserTestUtils.CreateUser("test-user");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var query = new GetUserByUsernameQuery(new Username("test-user"));

        // Act

        var result = await _handler.HandleAsync(query);

        // Assert

        result.Username.Should().Be("test-user");
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email.Value);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.IsEmailVerified.Should().Be(user.IsEmailVerified);
    }
}
