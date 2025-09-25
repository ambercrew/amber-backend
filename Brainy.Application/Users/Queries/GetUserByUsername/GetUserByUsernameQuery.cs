using Brainy.Application.Users.Dto;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Queries.Abstractions;

namespace Brainy.Application.Users.Queries.GetUserByUsername;

public record GetUserByUsernameQuery(Username Username) : IQuery<UserInformationDto>;
