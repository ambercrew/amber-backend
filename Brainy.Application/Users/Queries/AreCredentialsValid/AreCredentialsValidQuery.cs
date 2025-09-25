using Brainy.Application.Queries.AreCredentialsValid;
using LiteBus.Queries.Abstractions;

namespace Brainy.Application.Users.Queries.AreCredentialsValid;

public record AreCredentialsValidQuery(SignInDto SignInDto) : IQuery<bool>;
