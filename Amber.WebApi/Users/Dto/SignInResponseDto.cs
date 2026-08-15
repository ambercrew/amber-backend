using Amber.Application.Users.Dto;

namespace Amber.WebApi.Users.Dto;

public record SignInResponseDto(UserInformationDto User, string Token);
