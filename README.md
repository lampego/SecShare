# SecShare

SecShare is a secure file and secret sharing tool for developers, built around a CLI-first workflow. It lets you upload a file, directory, or text secret from the terminal, get a link, and send that link to another person.

The project is named SecShare, while the current CLI command and release assets use the `devshare` name.

## Why

Sending secrets through Slack, Telegram, or email is easy, but it leaves sensitive data in places where it does not belong. SecShare is meant for short-lived sharing flows that are convenient from a terminal and easy to automate in scripts or CI jobs.

## Features

- CLI upload flow for files and directories.
- Text secret sharing through CLI text mode.
- Download and decrypt shared content from a link.
- Client-side AES-256-GCM encryption in the CLI.
- Linux, macOS, and Windows installer scripts.
- TTL, download limits, password prompts, and delete-after-read enforcement: planned. The CLI already sends some related metadata, but backend enforcement is not complete yet.

## Installation

Linux and macOS:

```bash
curl -fsSL https://devshare.me/install.sh | sh
```

Windows:

```powershell
irm https://devshare.me/install.ps1 | iex
```

See [Installation Guide](docs/install.md).

Uninstall commands are also available in the installation guide.

## Usage Examples

Upload a file:

```bash
devshare upload ./backup.zip
```

Upload a directory:

```bash
devshare upload ./logs
```

Share a text secret:

```bash
devshare upload "DATABASE_URL=postgres://user:pass@example/db" --text
```

Send expiry/download metadata with an upload:

```bash
devshare upload ./report.pdf --expires 1h --downloads 1
```

Download and decrypt a shared link:

```bash
devshare get "https://secshare.me/f/<token>#<key>" ./downloads
```
## Security Note

SecShare is intended to make temporary link-based sharing safer and more convenient, but it should not be treated as an absolute security guarantee. The CLI encrypts uploaded content client-side with AES-256-GCM and puts the decryption key in the URL fragment, which is not sent to the API during normal HTTP requests.

Do not post production secrets in GitHub issues, discussions, logs, or screenshots. TTL, download-limit, password, and delete-after-read behavior should be treated as planned until backend enforcement is complete.

## License

This project is licensed under the Apache License 2.0.
See [LICENSE](LICENSE) for details.
