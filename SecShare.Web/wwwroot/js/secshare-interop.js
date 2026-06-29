window.secshareInterop = {
    getLocationHash: function () {
        const hash = window.location.hash;
        if (!hash || hash.length <= 1) {
            return null;
        }
        // Return raw value after '#'; URI decoding happens on the C# side if needed.
        return hash.substring(1);
    },

    clearLocationHash: function () {
        if (window.location.hash) {
            try {
                const cleanUrl = window.location.pathname + window.location.search;
                history.replaceState(null, document.title, cleanUrl);
            } catch (_) {
                // Ignore – some environments block history manipulation.
            }
        }
    },

    saveFile: function (fileName, contentBytes) {
        try {
            const blob = new Blob([contentBytes]);
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName;
            anchor.style.display = 'none';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            // Revoke after a short delay to let the browser initiate the download.
            setTimeout(() => URL.revokeObjectURL(url), 60_000);
        } catch (err) {
            // Do not surface internal details to the console in production.
            console.warn('SecShare: file save failed.');
        }
    },

    copyText: function (text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text).catch(() => {
                this._fallbackCopy(text);
            });
        }
        this._fallbackCopy(text);
        return Promise.resolve();
    },

    _fallbackCopy: function (text) {
        try {
            const textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.cssText = 'position:fixed;opacity:0;top:0;left:0;width:1px;height:1px';
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            document.execCommand('copy');
            document.body.removeChild(textarea);
        } catch (_) {
            // Ignore fallback failures silently.
        }
    },

    // Navigate to url preserving the fragment so the Receive page can read #key.
    navigateTo: function (url) {
        window.location.href = url;
    }
};


