namespace AcmeCoffeeOnline.Playwright.Infrastructure;

[CollectionDefinition(Name)]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "playwright";
}
