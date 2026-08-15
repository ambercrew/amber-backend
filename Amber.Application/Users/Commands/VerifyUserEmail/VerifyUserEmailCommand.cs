using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.VerifyUserEmail;

public record VerifyUserEmailCommand(Username Username, VerifyEmailDto VerifyEmailDto) : ICommand;
