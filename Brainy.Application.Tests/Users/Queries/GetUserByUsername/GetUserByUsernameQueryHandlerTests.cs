using Brainy.Application.Users.Queries.GetUserByUsername;
using Brainy.Domain.Users.ValueObjects;
using Brainy.Infrastructure.Users.Repositories;
using Brainy.TestUtils;
using Brainy.TestUtils.Users;

namespace Brainy.Application.Tests.Users.Queries.GetUserByUsername;

[TestClass]
public class GetUserByUsernameQueryHandlerTests : RepositoryTestBase
{
    private GetUserByUsernameQueryHandler _handler = null!;
    private UserRepository _userRepository = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepository = new UserRepository(BrainyContext);
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
