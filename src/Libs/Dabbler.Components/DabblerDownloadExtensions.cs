// Copyright (c) Dabbler 2025
// Licensed under the MIT license.

using Microsoft.JSInterop;

namespace Dabbler.Components;

/// <summary>
/// Browser file download through the <c>dabbler-download.js</c> module that
/// ships in this package's static web assets.
/// </summary>
public static class DabblerDownloadExtensions
{
    private const string ModulePath = "./_content/Dabbler.Components/dabbler-download.js";

    /// <summary>
    /// Saves <paramref name="content"/> in the browser as a file named
    /// <paramref name="fileName"/>.
    /// </summary>
    /// <remarks>
    /// The content crosses the circuit as a base64 string in a single
    /// JS interop call, so it is held in memory on both sides; serve a large
    /// file from an HTTP endpoint instead.
    /// </remarks>
    /// <param name="js">The JS runtime of the current circuit.</param>
    /// <param name="fileName">The name the browser saves the file under.</param>
    /// <param name="content">The file's bytes.</param>
    /// <param name="mimeType">The file's MIME type, for example <c>text/csv</c>.</param>
    /// <param name="cancellationToken">Cancels the JS interop calls.</param>
    public static async Task DownloadFileAsync(
        this IJSRuntime js,
        string fileName,
        byte[] content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(js);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(mimeType);

        await using var module = await js.InvokeAsync<IJSObjectReference>(
            "import", cancellationToken, ModulePath);
        await module.InvokeVoidAsync(
            "downloadFile", cancellationToken, fileName, Convert.ToBase64String(content), mimeType);
    }
}
