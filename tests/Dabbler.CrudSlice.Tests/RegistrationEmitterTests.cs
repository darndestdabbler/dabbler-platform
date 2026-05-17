using FluentAssertions;
using Xunit;

namespace Dabbler.CrudSlice.Tests;

/// <summary>
/// Pure-substitution unit tests for <see cref="RegistrationEmitter"/>. These tests
/// do not invoke the `dotnet new` templates or the schema applicator — they only
/// verify the rendered registration helpers' shape against the spec's contract
/// (Session 2 of crud-scaffolding-remediation, Step 5):
///
///   * Generated `{Entity}ScaffoldingExtensions.cs` contains `Services:ApiBaseUrl`,
///     `X-Internal-Api-Key`, `UserIdentityHandler`.
///   * Generated extensions do NOT contain `Api:BaseUrl` (the wrong key).
///   * Generated `{Entity}ApiRegistrationExtensions.cs` registers
///     `I{Entity}Service -> {Entity}Service` as scoped.
/// </summary>
public class RegistrationEmitterTests
{
    private static CliOptions SampleOpts(string entity = "Smoke", string entityPlural = "Smokes") =>
        new(
            Entity: entity,
            EntityPlural: entityPlural,
            SchemaPath: "smoke.dabbler-entity.json",
            WebRoot: "/tmp/Web",
            ApiRoot: "/tmp/Api",
            WebNamespace: "AcmeCoffeeOnline.Web",
            ApiNamespace: "AcmeCoffeeOnline.Api",
            SharedNamespace: "AcmeCoffeeOnline.Shared",
            DtoNamespace: "AcmeCoffeeOnline.Shared.DTOs",
            EntityNamespace: "AcmeCoffeeOnline.Shared.Models",
            DbContextType: "AcmeCoffeeDbContext",
            DbContextNamespace: "AcmeCoffeeOnline.Api.Data",
            PkType: "int",
            AuthPolicyPrefix: null,
            ApiBaseUrlConfigKey: "Services:ApiBaseUrl",
            ApiBaseUrlFallbackConfigKey: "ApiBaseUrl",
            ApiBaseUrlDefault: "http://localhost:5011",
            InternalApiKeyConfigKey: "DabblerSecurity:InternalApiKey",
            InternalApiKeyDefault: "dev-internal-key-acme",
            TemplateHive: null,
            SkipTemplates: false,
            SkipSchemaApplicator: false,
            SkipRegistrations: false);

    [Fact]
    public void Web_extension_uses_correct_config_key_internal_api_key_and_user_identity_handler()
    {
        var rendered = RegistrationEmitter.RenderWebScaffoldingExtensions(SampleOpts());

        rendered.Should().Contain("\"Services:ApiBaseUrl\"",
            "the wrapper-emitted extension must read Services:ApiBaseUrl first (matches Web/Program.cs:291)");
        rendered.Should().Contain("\"ApiBaseUrl\"",
            "the wrapper-emitted extension must fall back to bare ApiBaseUrl key (matches Web/Program.cs:292)");
        rendered.Should().Contain("\"http://localhost:5011\"",
            "the wrapper-emitted extension must keep the hard default that matches Web/Program.cs:293");
        rendered.Should().Contain("\"DabblerSecurity:InternalApiKey\"",
            "the wrapper-emitted extension must read the internal-api-key from DabblerSecurity:InternalApiKey (matches Web/Program.cs:294)");
        rendered.Should().Contain("\"X-Internal-Api-Key\"",
            "the wrapper-emitted extension must set the X-Internal-Api-Key request header");
        rendered.Should().Contain(".AddHttpMessageHandler<UserIdentityHandler>()",
            "the wrapper-emitted extension must attach UserIdentityHandler for per-request user identity forwarding");
        rendered.Should().Contain("using Dabbler.Security.Blazor.Extensions;",
            "the using directive for UserIdentityHandler must be emitted");
    }

    [Fact]
    public void Web_extension_must_not_use_the_wrong_config_key()
    {
        var rendered = RegistrationEmitter.RenderWebScaffoldingExtensions(SampleOpts());

        rendered.Should().NotContain("\"Api:BaseUrl\"",
            "Api:BaseUrl is the wrong config key — the host reads Services:ApiBaseUrl. " +
            "This was the exemplar bug that motivated the remediation spec.");
    }

    [Fact]
    public void Web_extension_registers_typed_http_client_and_grid_data_provider()
    {
        var rendered = RegistrationEmitter.RenderWebScaffoldingExtensions(SampleOpts("Supplier", "Suppliers"));

        rendered.Should().Contain("public static IHttpClientBuilder AddSupplierScaffolding(");
        rendered.Should().Contain("services.AddHttpClient<SupplierApiClient>(");
        rendered.Should().Contain("services.AddTransient<IGridDataProvider<SupplierDto>, SupplierGridDataProvider>();");
        rendered.Should().Contain("namespace AcmeCoffeeOnline.Web.Data;");
    }

    [Fact]
    public void Api_extension_registers_scoped_service()
    {
        var rendered = RegistrationEmitter.RenderApiRegistrationExtensions(SampleOpts("Supplier", "Suppliers"));

        rendered.Should().Contain("public static IServiceCollection AddSupplierApiServices(",
            "the API-side extension must expose Add{Entity}ApiServices as the one-liner");
        rendered.Should().Contain("services.AddScoped<ISupplierService, SupplierService>();",
            "the API-side extension must register I{Entity}Service -> {Entity}Service as scoped (matches Api/Program.cs:162)");
        rendered.Should().Contain("namespace AcmeCoffeeOnline.Api.Services;");
    }

    [Fact]
    public void Honors_custom_config_keys_when_supplied()
    {
        var opts = SampleOpts() with
        {
            ApiBaseUrlConfigKey = "MyApp:Backend:Url",
            ApiBaseUrlFallbackConfigKey = "Backend:Url",
            ApiBaseUrlDefault = "https://prod.example.com",
            InternalApiKeyConfigKey = "MyApp:Shared:Secret",
            InternalApiKeyDefault = "override-default",
        };
        var rendered = RegistrationEmitter.RenderWebScaffoldingExtensions(opts);

        rendered.Should().Contain("\"MyApp:Backend:Url\"");
        rendered.Should().Contain("\"Backend:Url\"");
        rendered.Should().Contain("\"https://prod.example.com\"");
        rendered.Should().Contain("\"MyApp:Shared:Secret\"");
        rendered.Should().Contain("\"override-default\"");
        rendered.Should().NotContain("\"Services:ApiBaseUrl\"");
    }
}
