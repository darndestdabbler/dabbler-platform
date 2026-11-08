using ComponentCatalogCrawler;

var output = "docs/component-catalog";
var baseUrl = "http://localhost:5010";
var apiBaseUrl = "http://localhost:5011";
var testAuthSecret = "acme-local-seeded-session";
var internalApiKey = "dev-internal-key-acme";
var pages = "src/Apps/AcmeCoffeeOnline.Web/Components/Pages";
var seedEmail = "admin@acmecoffee.com";
var headless = true;
var roles = new[] { "SystemAdmin", "BackofficeUser", "CrossOrgMonitor", "CrossOrgUserAdmin", "User" };

for (int i = 0; i < args.Length; i++)
{
    var a = args[i];
    string Next() => ++i < args.Length
        ? args[i]
        : throw new ArgumentException($"Missing value for {a}");

    switch (a)
    {
        case "--output": output = Next(); break;
        case "--base-url": baseUrl = Next(); break;
        case "--api-base-url": apiBaseUrl = Next(); break;
        case "--test-auth-secret": testAuthSecret = Next(); break;
        case "--internal-api-key": internalApiKey = Next(); break;
        case "--pages-dir": pages = Next(); break;
        case "--seed-email": seedEmail = Next(); break;
        case "--roles": roles = Next().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); break;
        case "--headed": headless = false; break;
        case "--help" or "-h":
            PrintHelp();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown arg: {a}");
            PrintHelp();
            return 1;
    }
}

var opts = new CrawlerOptions(
    PagesDirectory: pages,
    BaseUrl: baseUrl,
    ApiBaseUrl: apiBaseUrl,
    TestAuthSecret: testAuthSecret,
    InternalApiKey: internalApiKey,
    OutputDirectory: output,
    Headless: headless,
    AppName: "AcmeCoffeeOnline",
    Roles: roles,
    SeedEmail: seedEmail);

var orchestrator = new CrawlerOrchestrator(opts);
return await orchestrator.RunAsync();

static void PrintHelp()
{
    Console.WriteLine("ComponentCatalogCrawler — produce a runtime UI catalog for AcmeCoffeeOnline.Web.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project tools/ComponentCatalogCrawler/ -- [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --output <dir>             Output directory (default: docs/component-catalog)");
    Console.WriteLine("  --base-url <url>           Web base URL (default: http://localhost:5010)");
    Console.WriteLine("  --api-base-url <url>       API base URL (default: http://localhost:5011)");
    Console.WriteLine("  --test-auth-secret <s>     Seeded-session shared secret (default: acme-local-seeded-session)");
    Console.WriteLine("  --internal-api-key <k>     Internal API key (default: dev-internal-key-acme)");
    Console.WriteLine("  --pages-dir <path>         Razor pages dir (default: src/Apps/AcmeCoffeeOnline.Web/Components/Pages)");
    Console.WriteLine("  --seed-email <email>       Email to authenticate with (default: admin@acmecoffee.com)");
    Console.WriteLine("  --roles <r1,r2,...>        Comma-separated roles to crawl");
    Console.WriteLine("  --headed                   Run Chromium visibly (default: headless)");
}
