using Brainy.Application.Services;

namespace Brainy.WebApi.IntegrationTests.Services;

/// <summary>
/// Used instead of the default random number generator to allow testing.
/// </summary>
public class TestRandomGenerator : IRandomGenerator
{
    public const string ReturnValue = "12345678";

    /// <summary>
    /// This method always return the value in <see cref="ReturnValue" />.
    /// </summary>
    public string GenerateRandomAlphanumeric(int length)
    {
        return ReturnValue;
    }
}
