// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Dabbler.Components;

/// <summary>
/// Internal dismissible error banner used by page-level components.
/// </summary>
public partial class DabblerPageErrorBanner : DabblerComponentBase
{
    /// <summary>
    /// The error to display. When null, the banner is hidden.
    /// </summary>
    [Parameter]
    public Exception? Error { get; set; }

    /// <summary>
    /// Raised when the user dismisses the error.
    /// </summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private Task HandleDismissAsync()
    {
        return OnDismiss.InvokeAsync();
    }
}
