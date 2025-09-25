using Brainy.Application.Users.Dto;

namespace Brainy.WebApi.Users.Dto;

public record SignInResponseDto(UserInformationDto User, string Token);
