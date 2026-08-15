using Amber.Application.Users.Dto;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Queries.Abstractions;

namespace Amber.Application.Users.Queries.GetUserByUsername;

public record GetUserByUsernameQuery(Username Username) : IQuery<UserInformationDto>;
