using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Users.Commands.VerifyUserEmail;

public record VerifyUserEmailCommand(Username Username, VerifyEmailDto VerifyEmailDto) : ICommand;
