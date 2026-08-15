using Amber.Application.Users.Dto;
using Amber.Domain.Users.Repositories;
using LiteBus.Queries.Abstractions;

namespace Amber.Application.Users.Queries.GetUserByUsername;

public class GetUserByUsernameQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserByUsernameQuery, UserInformationDto>
{
    public async Task<UserInformationDto> HandleAsync(
        GetUserByUsernameQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var user = await userRepository.GetUserByUsernameAsync(query.Username);
        return UserInformationDto.FromUser(user);
    }
}
