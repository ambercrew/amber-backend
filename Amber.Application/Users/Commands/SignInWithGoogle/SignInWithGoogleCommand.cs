using Amber.Application.Users.Dto;
using LiteBus.Commands.Abstractions;

namespace Amber.Application.Users.Commands.SignInWithGoogle;

public record SignInWithGoogleCommand(GoogleSignInDto GoogleSignInDto)
    : ICommand<UserInformationDto>;
