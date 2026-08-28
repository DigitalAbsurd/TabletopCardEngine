/**
 * Downloads a file with the given content as base64-encoded data.
 * @param {string} base64Content - The file content encoded as base64
 * @param {string} fileName - The name of the file to download
 * @param {string} mimeType - The MIME type of the file (e.g., "text/plain")
 */
function downloadFile(base64Content, fileName, mimeType) {
    // Decode base64 to binary
    const binaryString = atob(base64Content);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }

    // Create blob from binary data
    const blob = new Blob([bytes], { type: mimeType });

    // Create object URL and trigger download
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}
