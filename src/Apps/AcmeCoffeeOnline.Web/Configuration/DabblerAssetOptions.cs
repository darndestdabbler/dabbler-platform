using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Configuration;

public sealed class DabblerAssetOptions
{
    public const string SectionName = "DabblerAssets";

    public string[] RequiredStyleAssets { get; set; } = [];
}

public sealed class DabblerAssetOptionsValidator : IValidateOptions<DabblerAssetOptions>
{
    private readonly IWebHostEnvironment _environment;

    public DabblerAssetOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, DabblerAssetOptions options)
    {
        var errors = new List<string>();

        if (options.RequiredStyleAssets.Length == 0)
        {
            errors.Add("DabblerAssets:RequiredStyleAssets must contain at least one stylesheet.");
        }

        foreach (var asset in options.RequiredStyleAssets)
        {
            if (string.IsNullOrWhiteSpace(asset))
            {
                errors.Add("DabblerAssets:RequiredStyleAssets cannot contain blank values.");
                continue;
            }

            var normalized = asset.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, normalized);
            if (!File.Exists(fullPath))
            {
                errors.Add($"Required Dabbler asset '{asset}' was not found under wwwroot.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
