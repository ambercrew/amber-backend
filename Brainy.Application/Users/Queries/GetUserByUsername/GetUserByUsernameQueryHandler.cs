using Brainy.Application.Users.Dto;
using Brainy.Domain.Users.Repositories;
using LiteBus.Queries.Abstractions;

namespace Brainy.Application.Users.Queries.GetUserByUsername;

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
