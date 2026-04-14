// Clipboard and file-download JS interop for DabblerDataGrid.

/**
 * Writes text to the system clipboard.
 * @param {string} text - The text to copy.
 * @returns {Promise<boolean>} True if successful.
 */
export async function writeText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        // Fallback for older browsers or insecure contexts
        const textarea = document.createElement("textarea");
        textarea.value = text;
        textarea.style.position = "fixed";
        textarea.style.opacity = "0";
        document.body.appendChild(textarea);
        textarea.select();
        try {
            document.execCommand("copy");
            return true;
        } finally {
            document.body.removeChild(textarea);
        }
    }
}

/**
 * Reads text from the system clipboard.
 * @returns {Promise<string|null>} The clipboard text, or null if unavailable.
 */
export async function readText() {
    try {
        return await navigator.clipboard.readText();
    } catch {
        return null;
    }
}

/**
 * Triggers a browser file download from a base64-encoded string.
 * @param {string} fileName - The suggested file name.
 * @param {string} base64Data - The file content as a base64 string.
 * @param {string} mimeType - The MIME type (e.g., "text/csv").
 */
export function downloadFile(fileName, base64Data, mimeType) {
    const byteChars = atob(base64Data);
    const byteArray = new Uint8Array(byteChars.length);
    for (let i = 0; i < byteChars.length; i++) {
        byteArray[i] = byteChars.charCodeAt(i);
    }
    const blob = new Blob([byteArray], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}
