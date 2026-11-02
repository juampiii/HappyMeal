namespace Happymeal.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class HappymealCollection : ICollectionFixture<HappymealApiFactory>
{
    public const string Name = "Happymeal API";
}
