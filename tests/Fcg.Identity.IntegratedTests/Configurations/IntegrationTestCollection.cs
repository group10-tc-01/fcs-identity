namespace Fcg.Identity.IntegratedTests.Configurations;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}
