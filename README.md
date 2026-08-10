# SecShare — CLI-first encrypted file sharing

SecShare lets you send a file, directory, or text secret with a temporary link. It encrypts the content on your machine before upload; the decryption key stays in the link fragment and is not sent to SecShare in a normal HTTP request.

Website: [secshare.me](https://secshare.me/)

The CLI is the primary workflow. A recipient can also open a link in the browser and decrypt the content locally.

## Why

Pasting a production credential into Slack, Telegram, or email leaves a durable copy in chat history, search indexes, notifications, and backups. SecShare is for sharing sensitive content from a terminal with a defined expiry and download limit.

## Quick start

Linux and macOS:

```bash
curl -fsSL https://secshare.me/install.sh | sh
```

Windows:

```powershell
irm https://secshare.me/install.ps1 | iex
```

Create a link that expires in one hour after one download:

```bash
secshare upload ./backup.zip --expires 1h --downloads 1
```

The command prints a link such as `https://secshare.me/f/<token>#<decryption-key>`. Send it to the recipient, who can use the CLI or open it in a browser.

For supported platforms, version pinning, checksums, and uninstalling, see the [installation guide](docs/install.md).

## Usage

Upload a file:

```bash
secshare upload ./backup.zip
```

Upload a directory recursively. SecShare packages it as an encrypted archive and extracts it after download:

```bash
secshare upload ./logs
```

Share a text secret:

```bash
secshare upload "DATABASE_URL=postgres://user:pass@example/db" --text
```

Pass text from a script or CI job through standard input:

```bash
printf '%s' "$DEPLOY_TOKEN" | secshare upload
```

Download and decrypt a complete link into a destination directory:

```bash
secshare get "https://secshare.me/f/<token>#<decryption-key>" ./downloads
```

To share the URL and key through separate channels, give `secshare get` the URL without the fragment. It prompts for the key:

```bash
secshare get "https://secshare.me/f/<token>" ./downloads
```

See the complete [CLI usage guide](docs/cli-usage.md).

## Security model

1. SecShare packages the selected content locally and generates a random 32-byte key.
2. The package is encrypted locally with AES-256-GCM, using a random 12-byte nonce and a 16-byte authentication tag.
3. SecShare receives the encrypted payload, a share token, expiry, download limit, and metadata needed to operate the link. It does not receive the plaintext or decryption key.
4. The key is placed after `#` in the share URL. URL fragments are not included in normal HTTP requests, so a request to SecShare contains the token but not the key.
5. The recipient downloads the encrypted payload and decrypts it locally in the CLI or browser. AES-GCM authentication rejects altered data and incorrect keys.

### What to keep in mind

- Anyone with the complete link can decrypt the content. Treat it as a secret: it can leak through shell history, screenshots, clipboard sync, chat forwarding, browser extensions, or application logs.
- Send the URL and its key separately when you need an additional separation of channels.
- Expiry and download limits are enforced by the service. A download request consumes one allowed download; after the limit is reached or the expiry passes, the encrypted payload is scheduled for deletion. These controls reduce exposure but are not instantaneous cryptographic erasure.
- This is a client-side encryption design, not a substitute for an independent security audit or protection against a compromised device, client, or delivery channel.

## Technical details

- The CLI encrypts files, directories, and text locally with AES-256-GCM before upload.
- A file or directory is transferred as an encrypted archive; the original content is restored after decryption.
- The browser can download and decrypt a received link locally.

## License

Licensed under the [Apache License 2.0](LICENSE).
