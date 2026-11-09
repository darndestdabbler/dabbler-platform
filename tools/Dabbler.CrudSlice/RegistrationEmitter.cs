using System.Text;

namespace Dabbler.CrudSlice;

/// <summary>
/// Emits convention-correct registration helper files for a scaffolded CRUD slice.
/// The two helpers replace the wrong-convention surfaces produced by the
/// dabbler-crud-page template (whose default <c>{Entity}ScaffoldingExtensions.cs</c>
/// reads the wrong config key, omits the X-Internal-Api-Key header, and omits
/// UserIdentityHandler) and supply a dedicated API-side DI registration helper
/// that the consumer can call from <c>Api/Program.cs</c>.
///
/// The shapes match the real production wiring in
/// <c>src/Apps/AcmeCoffeeOnline.Web/Program.cs:291-330</c> and
/// <c>src/Apps/AcmeCoffeeOnline.Api/Program.cs:156-162</c>.
/// </summary>
internal static class RegistrationEmitter
{
    public static string RenderWebScaffoldingExtensions(CliOptions opts)
    {
        var sb = new StringBuilder();
        sb.Append("using Dabbler.Components.Data;\n");
        sb.Append("using Dabbler.Security.Blazor.Extensions;\n");
        sb.Append($"using {opts.DtoNamespace};\n");
        sb.Append("using Microsoft.Extensions.Configuration;\n");
        sb.Append("using Microsoft.Extensions.DependencyInjection;\n");
        sb.Append('\n');
        sb.Append($"namespace {opts.WebNamespace}.Data;\n");
        sb.Append('\n');
        sb.Append("/// <summary>\n");
        sb.Append($"/// One-line wire-up for the scaffolded {opts.Entity} slice. Call from Program.cs once after\n");
        sb.Append("/// pre-staging the DTO and EF entity types:\n");
        sb.Append("///\n");
        sb.Append("/// <code>\n");
        sb.Append($"/// builder.Services.Add{opts.Entity}Scaffolding(builder.Configuration);\n");
        sb.Append("/// </code>\n");
        sb.Append("///\n");
        sb.Append("/// Emitted by Dabbler.CrudSlice — matches the host's real production wiring:\n");
        sb.Append($"///   * Base URL: configuration[\"{opts.ApiBaseUrlConfigKey}\"]\n");
        sb.Append($"///         ?? configuration[\"{opts.ApiBaseUrlFallbackConfigKey}\"]\n");
        sb.Append($"///         ?? \"{opts.ApiBaseUrlDefault}\"\n");
        sb.Append($"///   * Header X-Internal-Api-Key: configuration[\"{opts.InternalApiKeyConfigKey}\"]\n");
        sb.Append($"///         ?? \"{opts.InternalApiKeyDefault}\"\n");
        sb.Append("///   * AddHttpMessageHandler&lt;UserIdentityHandler&gt;() for per-request user identity forwarding\n");
        sb.Append("/// </summary>\n");
        sb.Append($"public static class {opts.Entity}ScaffoldingExtensions\n");
        sb.Append("{\n");
        sb.Append($"    /// <summary>Registers the scaffolded {opts.Entity} slice's API client and grid data provider.</summary>\n");
        sb.Append($"    public static IHttpClientBuilder Add{opts.Entity}Scaffolding(\n");
        sb.Append("        this IServiceCollection services,\n");
        sb.Append("        IConfiguration configuration)\n");
        sb.Append("    {\n");
        sb.Append($"        var baseUrl = configuration[\"{opts.ApiBaseUrlConfigKey}\"]\n");
        sb.Append($"            ?? configuration[\"{opts.ApiBaseUrlFallbackConfigKey}\"]\n");
        sb.Append($"            ?? \"{opts.ApiBaseUrlDefault}\";\n");
        sb.Append($"        var internalApiKey = configuration[\"{opts.InternalApiKeyConfigKey}\"]\n");
        sb.Append($"            ?? \"{opts.InternalApiKeyDefault}\";\n");
        sb.Append('\n');
        sb.Append($"        var httpBuilder = services.AddHttpClient<{opts.Entity}ApiClient>(client =>\n");
        sb.Append("        {\n");
        sb.Append("            client.BaseAddress = new Uri(baseUrl);\n");
        sb.Append("            client.DefaultRequestHeaders.Add(\"X-Internal-Api-Key\", internalApiKey);\n");
        sb.Append("        }).AddHttpMessageHandler<UserIdentityHandler>();\n");
        sb.Append('\n');
        sb.Append($"        services.AddTransient<IGridDataProvider<{opts.Entity}Dto>, {opts.Entity}GridDataProvider>();\n");
        sb.Append('\n');
        sb.Append("        return httpBuilder;\n");
        sb.Append("    }\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    public static string RenderApiRegistrationExtensions(CliOptions opts)
    {
        var sb = new StringBuilder();
        sb.Append("using Microsoft.Extensions.DependencyInjection;\n");
        sb.Append('\n');
        sb.Append($"namespace {opts.ApiNamespace}.Services;\n");
        sb.Append('\n');
        sb.Append("/// <summary>\n");
        sb.Append($"/// One-line wire-up for the scaffolded {opts.Entity} service. Call from Api/Program.cs:\n");
        sb.Append("///\n");
        sb.Append("/// <code>\n");
        sb.Append($"/// builder.Services.Add{opts.Entity}ApiServices();\n");
        sb.Append("/// </code>\n");
        sb.Append("///\n");
        sb.Append($"/// Emitted by Dabbler.CrudSlice — matches the host's real production wiring at\n");
        sb.Append("/// src/Apps/AcmeCoffeeOnline.Api/Program.cs:156-162.\n");
        sb.Append("/// </summary>\n");
        sb.Append($"public static class {opts.Entity}ApiRegistrationExtensions\n");
        sb.Append("{\n");
        sb.Append($"    /// <summary>Registers I{opts.Entity}Service -&gt; {opts.Entity}Service in DI (scoped).</summary>\n");
        sb.Append($"    public static IServiceCollection Add{opts.Entity}ApiServices(this IServiceCollection services)\n");
        sb.Append("    {\n");
        sb.Append($"        services.AddScoped<I{opts.Entity}Service, {opts.Entity}Service>();\n");
        sb.Append("        return services;\n");
        sb.Append("    }\n");
        sb.Append("}\n");
        return sb.ToString();
    }
}
