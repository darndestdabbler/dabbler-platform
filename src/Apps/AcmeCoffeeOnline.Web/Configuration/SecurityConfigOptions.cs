using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Configuration;

public sealed class SecurityConfigOptions
{
    public const string SectionName = "SecurityConfig";

    public SecurityPolicyDefinition[] Policies { get; set; } = [];

    public SecurityPageDefinition[] Pages { get; set; } = [];

    public SecurityEndpointDefinition[] Endpoints { get; set; } = [];
}

public sealed class SecurityPolicyDefinition
{
    public string Name { get; set; } = string.Empty;

    public string[] Roles { get; set; } = [];
}

public sealed class SecurityPageDefinition
{
    public string Route { get; set; } = string.Empty;

    public bool AllowAnonymous { get; set; }
}

public sealed class SecurityEndpointDefinition
{
    public string Pattern { get; set; } = string.Empty;

    public string Policy { get; set; } = string.Empty;
}

public sealed class SecurityConfigOptionsValidator : IValidateOptions<SecurityConfigOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityConfigOptions options)
    {
        var errors = new List<string>();

        if (options.Policies.Length == 0)
        {
            errors.Add("SecurityConfig:Policies must define at least one policy.");
        }

        var requiredAnonymousPages = new[] { "/login", "/access-denied" };
        foreach (var route in requiredAnonymousPages)
        {
            var page = options.Pages.FirstOrDefault(candidate =>
                string.Equals(candidate.Route, route, StringComparison.OrdinalIgnoreCase));

            if (page is null)
            {
                errors.Add($"SecurityConfig:Pages must include '{route}'.");
                continue;
            }

            if (!page.AllowAnonymous)
            {
                errors.Add($"SecurityConfig:Pages '{route}' must be marked AllowAnonymous.");
            }
        }

        foreach (var policy in options.Policies)
        {
            if (string.IsNullOrWhiteSpace(policy.Name))
            {
                errors.Add("SecurityConfig:Policies entries must have a non-empty Name.");
            }

            if (policy.Roles.Length == 0)
            {
                errors.Add($"SecurityConfig policy '{policy.Name}' must declare at least one role.");
            }
        }

        foreach (var endpoint in options.Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Pattern))
            {
                errors.Add("SecurityConfig:Endpoints entries must have a Pattern.");
            }

            if (!options.Policies.Any(policy =>
                    string.Equals(policy.Name, endpoint.Policy, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(
                    $"SecurityConfig endpoint '{endpoint.Pattern}' references missing policy '{endpoint.Policy}'.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
