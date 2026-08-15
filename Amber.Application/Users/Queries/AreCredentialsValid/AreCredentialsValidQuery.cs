using Amber.Application.Queries.AreCredentialsValid;
using LiteBus.Queries.Abstractions;

namespace Amber.Application.Users.Queries.AreCredentialsValid;

public record AreCredentialsValidQuery(SignInDto SignInDto) : IQuery<bool>;
