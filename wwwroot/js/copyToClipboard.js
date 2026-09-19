// Kopierar text till urklipp åt Blazor-kretsen, som inte når urklippet själv.
window.copyToClipboard = function (text) {
    return navigator.clipboard.writeText(text);
};
