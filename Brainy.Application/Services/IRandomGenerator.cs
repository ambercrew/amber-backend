namespace Brainy.Application.Services;

public interface IRandomGenerator : IApplicationService
{
    /// <summary>
    /// Generates a random alphanumeric string with the specified length,
    /// consisting only of uppercase character and numbers.
    /// </summary>
    string GenerateRandomAlphanumeric(int length);
}
