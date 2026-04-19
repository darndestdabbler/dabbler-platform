using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Configuration;

public enum AuthenticationDeliveryMode
{
    MagicLink,
    ExternalOidc
}

public sealed class AppAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string DeliveryMode { get; set; } = AuthenticationDeliveryMode.MagicLink.ToString();

    public bool ShowSsoButton { get; set; }

    public SeededSessionOptions SeededSession { get; set; } = new();
}

public sealed class SeededSessionOptions
{
    public bool Enabled { get; set; }

    public string SharedSecret { get; set; } = string.Empty;

    public string[] AllowedRoles { get; set; } = [];
}

public sealed class AppAuthenticationOptionsValidator : IValidateOptions<AppAuthenticationOptions>
{
    private readonly IHostEnvironment _environment;

    public AppAuthenticationOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AppAuthenticationOptions options)
    {
        var errors = new List<string>();

        if (!Enum.TryParse<AuthenticationDeliveryMode>(options.DeliveryMode, ignoreCase: true, out _))
        {
            errors.Add(
                "Authentication:DeliveryMode must be one of: "
                + string.Join(", ", Enum.GetNames<AuthenticationDeliveryMode>()) + ".");
        }

        if (options.SeededSession.Enabled)
        {
            if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Test"))
            {
                errors.Add("Authentication:SeededSession:Enabled can only be true in Development or Test.");
            }

            if (string.IsNullOrWhiteSpace(options.SeededSession.SharedSecret))
            {
                errors.Add("Authentication:SeededSession:SharedSecret is required when seeded sessions are enabled.");
            }

            if (options.SeededSession.AllowedRoles.Length == 0)
            {
                errors.Add("Authentication:SeededSession:AllowedRoles must contain at least one role.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
