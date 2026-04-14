using Microsoft.Extensions.Options;

namespace AcmeCoffeeOnline.Web.Configuration;

public sealed class DabblerAssetOptions
{
    public const string SectionName = "DabblerAssets";

    public DabblerStyleAsset[] Styles { get; set; } = [];
}

public sealed class DabblerStyleAsset
{
    public string AssetId { get; set; } = string.Empty;

    public string Href { get; set; } = string.Empty;
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

        if (options.Styles.Length == 0)
        {
            errors.Add("DabblerAssets:RequiredStyleAssets must contain at least one stylesheet.");
        }

        foreach (var asset in options.Styles)
        {
            if (string.IsNullOrWhiteSpace(asset.AssetId))
            {
                errors.Add("DabblerAssets:Styles:AssetId cannot be blank.");
            }

            if (string.IsNullOrWhiteSpace(asset.Href))
            {
                errors.Add("DabblerAssets:Styles:Href cannot be blank.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
