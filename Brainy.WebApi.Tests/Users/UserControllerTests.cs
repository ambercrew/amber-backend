using Brainy.Application.Users.Commands.DeleteUser;
using Brainy.Application.Users.Commands.UpdateUserInformation;
using Brainy.Application.Users.Dto;
using Brainy.Application.Users.Queries.GetUserByUsername;
using Brainy.Domain.Users.ValueObjects;
using Brainy.TestUtils.Users;
using Brainy.WebApi.Users;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Brainy.WebApi.Tests.Users;

[TestClass]
public class UserControllerTests
{
    private MockHttpContext _mockHttpContext = null!;
    private ICommandMediator _commandMediator = null!;
    private IQueryMediator _queryMediator = null!;
    private UserController _controller = null!;

    [TestInitialize]
    public void Initialize()
    {
        _commandMediator = Substitute.For<ICommandMediator>();
        _queryMediator = Substitute.For<IQueryMediator>();
        _controller = new(_queryMediator, _commandMediator);
        _mockHttpContext = new();
        _controller.ControllerContext.HttpContext = _mockHttpContext;
    }

    [TestMethod]
    public async Task GetUserInformationAsync_ValidInput_ReturnsOkWithUserDto()
    {
        // Arrange

        var username = new Username("test-user");
        var user = UserTestUtils.CreateUser(username.Value);
        var expectedDto = UserInformationDto.FromUser(user);

        _mockHttpContext.SetupSignedUser(username);
        _queryMediator.QueryAsync(new GetUserByUsernameQuery(username)).Returns(expectedDto);

        // Act

        var actual = (OkObjectResult)await _controller.GetUserInformationAsync();

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        actual.Value.Should().Be(expectedDto);
    }

    [TestMethod]
    public async Task UpdateUserInformationAsync_ValidInput_ReturnsOk()
    {
        // Arrange

        var username = new Username("test-user");
        var dto = new UpdateUserInformationDto("New first name", "New last name");

        _mockHttpContext.SetupSignedUser(username);

        // Act

        var actual = (IStatusCodeActionResult)await _controller.UpdateUserInformationAsync(dto);

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        await _commandMediator.Received().SendAsync(new UpdateUserCommand(dto, username));
    }

    [TestMethod]
    public async Task DeleteUserAsync_ValidInput_DeletesUser()
    {
        // Arrange

        var userId = Guid.NewGuid();
        _mockHttpContext.SetupSignedUser(id: userId);

        // Act

        var actual = (IStatusCodeActionResult)await _controller.DeleteUserAsync();

        // Assert

        actual.StatusCode.Should().Be(StatusCodes.Status200OK);
        await _commandMediator.Received().SendAsync(new DeleteUserCommand(userId));
    }
}
