namespace Amber.WebApi.IntegrationTests;

public class IntegrationTestBase
{
    private protected AmberWebApplicationFactory Factory = null!;
    private protected HttpClient HttpClient = null!;

    [TestInitialize]
    public void Initialize()
    {
        Factory = new();
        HttpClient = Factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Factory.Dispose();
        HttpClient.Dispose();
    }
}
