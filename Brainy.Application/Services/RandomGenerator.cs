using System.Text;

namespace Brainy.Application.Services;

public class RandomGenerator : IRandomGenerator
{
    // Define the pool of allowed characters (uppercase, and numbers)
    private const string CharsAndNumbers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public string GenerateRandomAlphanumeric(int length)
    {
        if (length <= 0)
            throw new ArgumentException("The length must be greater than zero.");

        var random = Random.Shared;

        var result = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            var randomIndex = random.Next(CharsAndNumbers.Length);
            result.Append(CharsAndNumbers[randomIndex]);
        }

        return result.ToString();
    }
}
