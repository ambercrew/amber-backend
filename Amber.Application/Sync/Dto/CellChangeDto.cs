namespace Amber.Application.Sync.Dto;

public record CellChangeDto(
    string Table,
    string RowId,
    string Column,
    byte[]? Value,
    string Hlc,
    string DeviceId
);
