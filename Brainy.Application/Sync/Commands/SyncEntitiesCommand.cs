using Brainy.Application.Sync.Dto;
using Brainy.Domain.Users.ValueObjects;
using LiteBus.Commands.Abstractions;

namespace Brainy.Application.Sync.Commands;

public record SyncEntitiesCommand(List<SyncEntityDto> Dto, Guid UserId, Username Username)
    : ICommand;
