namespace Dabbler.CrudSlice;

internal sealed record CliOptions(
    string Entity,
    string EntityPlural,
    string SchemaPath,
    string WebRoot,
    string ApiRoot,
    string WebNamespace,
    string ApiNamespace,
    string SharedNamespace,
    string DtoNamespace,
    string EntityNamespace,
    string DbContextType,
    string DbContextNamespace,
    string PkType,
    string? AuthPolicyPrefix,
    string ApiBaseUrlConfigKey,
    string ApiBaseUrlFallbackConfigKey,
    string ApiBaseUrlDefault,
    string InternalApiKeyConfigKey,
    string InternalApiKeyDefault,
    string? TemplateHive,
    bool SkipTemplates,
    bool SkipSchemaApplicator,
    bool SkipRegistrations)
{
    public static CliOptions Parse(string[] args)
    {
        string? entity = null;
        string? entityPlural = null;
        string? schema = null;
        string? webRoot = null;
        string? apiRoot = null;
        string? webNs = null;
        string? apiNs = null;
        string? sharedNs = null;
        string? dtoNs = null;
        string? entityNs = null;
        string? dbCtxType = null;
        string? dbCtxNs = null;
        string pkType = "int";
        string? authPolicyPrefix = null;
        string apiBaseUrlKey = "Services:ApiBaseUrl";
        string apiBaseUrlFallback = "ApiBaseUrl";
        string apiBaseUrlDefault = "http://localhost:5011";
        string internalApiKey = "DabblerSecurity:InternalApiKey";
        string internalApiKeyDefault = "dev-internal-key-acme";
        string? templateHive = null;
        bool skipTemplates = false;
        bool skipApplicator = false;
        bool skipRegistrations = false;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string? Next() => i + 1 < args.Length ? args[++i] : null;
            switch (a)
            {
                case "--entity":                          entity = Next(); break;
                case "--entity-plural":                   entityPlural = Next(); break;
                case "--schema":                          schema = Next(); break;
                case "--web-root":                        webRoot = Next(); break;
                case "--api-root":                        apiRoot = Next(); break;
                case "--web-namespace":                   webNs = Next(); break;
                case "--api-namespace":                   apiNs = Next(); break;
                case "--shared-namespace":                sharedNs = Next(); break;
                case "--dto-namespace":                   dtoNs = Next(); break;
                case "--entity-namespace":                entityNs = Next(); break;
                case "--dbcontext-type":                  dbCtxType = Next(); break;
                case "--dbcontext-namespace":             dbCtxNs = Next(); break;
                case "--pk-type":                         pkType = Next() ?? pkType; break;
                case "--auth-policy-prefix":              authPolicyPrefix = Next(); break;
                case "--api-base-url-config-key":         apiBaseUrlKey = Next() ?? apiBaseUrlKey; break;
                case "--api-base-url-fallback-config-key": apiBaseUrlFallback = Next() ?? apiBaseUrlFallback; break;
                case "--api-base-url-default":            apiBaseUrlDefault = Next() ?? apiBaseUrlDefault; break;
                case "--internal-api-key-config-key":     internalApiKey = Next() ?? internalApiKey; break;
                case "--internal-api-key-default":        internalApiKeyDefault = Next() ?? internalApiKeyDefault; break;
                case "--template-hive":                   templateHive = Next(); break;
                case "--skip-templates":                  skipTemplates = true; break;
                case "--skip-schema-applicator":          skipApplicator = true; break;
                case "--skip-registrations":              skipRegistrations = true; break;
                // --emit-registrations is a no-op alias for the default behavior (the
                // registration reference file is emitted by default; this flag just makes
                // the intent explicit on the command line for symmetry with --skip-*).
                // Pair with --skip-templates --skip-schema-applicator for emit-only mode.
                case "--emit-registrations":              skipRegistrations = false; break;
                case "-h":
                case "--help":
                    PrintUsage();
                    throw new HelpRequested();
                default:
                    throw new ArgumentException($"Unknown or incomplete argument: {a}");
            }
        }

        if (string.IsNullOrEmpty(entity))         throw new ArgumentException("Required: --entity <Singular>");
        if (string.IsNullOrEmpty(entityPlural))   throw new ArgumentException("Required: --entity-plural <Plural>");
        if (string.IsNullOrEmpty(schema))         throw new ArgumentException("Required: --schema <path>");
        if (string.IsNullOrEmpty(webRoot))        throw new ArgumentException("Required: --web-root <path>");
        if (string.IsNullOrEmpty(apiRoot))        throw new ArgumentException("Required: --api-root <path>");
        if (string.IsNullOrEmpty(webNs))          throw new ArgumentException("Required: --web-namespace <ns>");
        if (string.IsNullOrEmpty(apiNs))          throw new ArgumentException("Required: --api-namespace <ns>");
        if (string.IsNullOrEmpty(sharedNs))       throw new ArgumentException("Required: --shared-namespace <ns>");
        if (string.IsNullOrEmpty(dtoNs))          throw new ArgumentException("Required: --dto-namespace <ns>");
        if (string.IsNullOrEmpty(entityNs))       throw new ArgumentException("Required: --entity-namespace <ns>");
        if (string.IsNullOrEmpty(dbCtxType))      throw new ArgumentException("Required: --dbcontext-type <type>");
        if (string.IsNullOrEmpty(dbCtxNs))        throw new ArgumentException("Required: --dbcontext-namespace <ns>");

        return new CliOptions(
            entity, entityPlural, schema!, webRoot!, apiRoot!,
            webNs!, apiNs!, sharedNs!, dtoNs!, entityNs!,
            dbCtxType!, dbCtxNs!, pkType, authPolicyPrefix,
            apiBaseUrlKey, apiBaseUrlFallback, apiBaseUrlDefault,
            internalApiKey, internalApiKeyDefault,
            templateHive, skipTemplates, skipApplicator, skipRegistrations);
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Dabbler.CrudSlice — repo-aware CRUD scaffolding wrapper.

            Usage:
              Dabbler.CrudSlice
                --entity <Singular>
                --entity-plural <Plural>
                --schema <path-to-.dabbler-entity.json>
                --web-root <path-to-Web-project-root>
                --api-root <path-to-Api-project-root>
                --web-namespace <ns>
                --api-namespace <ns>
                --shared-namespace <ns>
                --dto-namespace <ns>
                --entity-namespace <ns>
                --dbcontext-type <ClassName>
                --dbcontext-namespace <ns>
                [--pk-type int]
                [--auth-policy-prefix <prefix>]
                [--api-base-url-config-key Services:ApiBaseUrl]
                [--api-base-url-fallback-config-key ApiBaseUrl]
                [--api-base-url-default http://localhost:5011]
                [--internal-api-key-config-key DabblerSecurity:InternalApiKey]
                [--internal-api-key-default dev-internal-key-acme]
                [--template-hive <path>]        (forwarded to dotnet new as --debug:custom-hive)
                [--skip-templates]              (emit registration helpers only)
                [--skip-schema-applicator]      (skip schema-driven substitution)
                [--skip-registrations]          (skip emitting {Entity}Registrations.g.cs copy-paste reference)
                [--emit-registrations]          (explicit no-op alias for default-on registrations emit)
            """);
    }
}

internal sealed class HelpRequested : Exception { }
