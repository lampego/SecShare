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

    generateAesKey: function () {
        const keySize = 32;
        const crypto = this._getWebCrypto();
        const keyBytes = new Uint8Array(keySize);
        crypto.getRandomValues(keyBytes);

        return this._base64UrlEncode(keyBytes);
    },

    encryptAesGcm: async function (dataBytes, base64UrlKey) {
        const nonceSize = 12;
        const tagSize = 16;
        const tagLength = tagSize * 8;
        const keySize = 32;
        const crypto = this._getWebCrypto();
        const data = this._toUint8Array(dataBytes);
        const keyBytes = this._base64UrlDecode(base64UrlKey);

        if (keyBytes.length !== keySize) {
            throw new Error('SecShareCrypto.InvalidKey: AES-256-GCM key must be exactly 32 bytes.');
        }

        const nonce = new Uint8Array(nonceSize);
        crypto.getRandomValues(nonce);

        try {
            const cryptoKey = await crypto.subtle.importKey(
                'raw',
                keyBytes,
                { name: 'AES-GCM' },
                false,
                ['encrypt']
            );
            const encrypted = new Uint8Array(await crypto.subtle.encrypt(
                {
                    name: 'AES-GCM',
                    iv: nonce,
                    tagLength: tagLength
                },
                cryptoKey,
                data
            ));
            const ciphertext = encrypted.slice(0, encrypted.length - tagSize);
            const tag = encrypted.slice(encrypted.length - tagSize);
            const payload = new Uint8Array(nonce.length + tag.length + ciphertext.length);
            payload.set(nonce, 0);
            payload.set(tag, nonce.length);
            payload.set(ciphertext, nonce.length + tag.length);

            return payload;
        } catch (err) {
            if (err && (err.name === 'NotSupportedError' || err.name === 'SecurityError')) {
                throw new Error('SecShareCrypto.Unsupported: Web Crypto AES-GCM is blocked or unsupported.');
            }

            throw err;
        }
    },

    decryptAesGcm: async function (payloadBytes, base64UrlKey) {
        const nonceSize = 12;
        const tagSize = 16;
        const tagLength = tagSize * 8;
        const keySize = 32;
        const crypto = this._getWebCrypto();
        const payload = this._toUint8Array(payloadBytes);

        if (payload.length < nonceSize + tagSize) {
            throw new Error('SecShareCrypto.InvalidPayload: encrypted payload is too short.');
        }

        const keyBytes = this._base64UrlDecode(base64UrlKey);
        if (keyBytes.length !== keySize) {
            throw new Error('SecShareCrypto.InvalidKey: AES-256-GCM key must be exactly 32 bytes.');
        }

        const nonce = payload.slice(0, nonceSize);
        const tag = payload.slice(nonceSize, nonceSize + tagSize);
        const ciphertext = payload.slice(nonceSize + tagSize);
        const ciphertextWithTag = new Uint8Array(ciphertext.length + tag.length);
        ciphertextWithTag.set(ciphertext, 0);
        ciphertextWithTag.set(tag, ciphertext.length);

        try {
            const cryptoKey = await crypto.subtle.importKey(
                'raw',
                keyBytes,
                { name: 'AES-GCM' },
                false,
                ['decrypt']
            );
            const plaintext = await crypto.subtle.decrypt(
                {
                    name: 'AES-GCM',
                    iv: nonce,
                    tagLength: tagLength
                },
                cryptoKey,
                ciphertextWithTag
            );

            return new Uint8Array(plaintext);
        } catch (err) {
            if (err && (err.name === 'NotSupportedError' || err.name === 'SecurityError')) {
                throw new Error('SecShareCrypto.Unsupported: Web Crypto AES-GCM is blocked or unsupported.');
            }

            throw new Error('SecShareCrypto.DecryptionFailed: payload authentication failed.');
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

    _toUint8Array: function (value) {
        if (value instanceof Uint8Array) {
            return value;
        }

        if (value instanceof ArrayBuffer) {
            return new Uint8Array(value);
        }

        return Uint8Array.from(value);
    },

    _getWebCrypto: function () {
        if (!globalThis.crypto || !globalThis.crypto.subtle) {
            throw new Error('SecShareCrypto.Unsupported: Web Crypto AES-GCM is unavailable.');
        }

        return globalThis.crypto;
    },

    _base64UrlEncode: function (bytes) {
        let binary = '';
        for (let i = 0; i < bytes.length; i++) {
            binary += String.fromCharCode(bytes[i]);
        }

        return btoa(binary)
            .replace(/=/g, '')
            .replace(/\+/g, '-')
            .replace(/\//g, '_');
    },

    _base64UrlDecode: function (value) {
        if (!value || typeof value !== 'string') {
            throw new Error('SecShareCrypto.InvalidKey: decryption key is required.');
        }

        const base64 = value
            .replace(/-/g, '+')
            .replace(/_/g, '/')
            .padEnd(value.length + ((4 - (value.length % 4)) % 4), '=');

        try {
            const binary = atob(base64);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                bytes[i] = binary.charCodeAt(i);
            }

            return bytes;
        } catch (err) {
            throw new Error('SecShareCrypto.InvalidKey: decryption key is not valid base64url.');
        }
    },

    // Navigate to url preserving the fragment so the Receive page can read #key.
    navigateTo: function (url) {
        window.location.href = url;
    }
};
