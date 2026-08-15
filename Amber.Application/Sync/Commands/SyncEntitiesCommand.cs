using Amber.Application.Sync.Dto;
using Amber.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Sync.Commands;

public record SyncEntitiesCommand(List<SyncEntityDto> Dto, Guid UserId, Username Username)
    : ICommand;
