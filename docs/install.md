# Installing SecShare CLI

The release assets are published from `lampego/SecShare` and use the `secshare-*` asset prefix:

- `secshare-linux-x64.tar.gz`
- `secshare-linux-arm64.tar.gz`
- `secshare-linux-musl-x64.tar.gz`
- `secshare-linux-musl-arm64.tar.gz`
- `secshare-osx-x64.tar.gz`
- `secshare-osx-arm64.tar.gz`
- `secshare-win-x64.zip`
- `checksums.txt`

The installer installs the command as `secshare` by default.

## Linux and macOS

```bash
curl -fsSL https://secshare.me/install.sh | sh
```

Install a specific release:

```bash
curl -fsSL https://secshare.me/install.sh | SECSHARE_VERSION=1.0.0.3 sh
```

Install without sudo:

```bash
curl -fsSL https://secshare.me/install.sh | SECSHARE_INSTALL_DIR="$HOME/.local/bin" sh
```

Uninstall:

```bash
curl -fsSL https://secshare.me/install.sh | sh -s -- uninstall
```

If you installed with a custom directory, pass the same `SECSHARE_INSTALL_DIR` value when uninstalling.

## Windows

```powershell
irm https://secshare.me/install.ps1 | iex
```

Install a specific release:

```powershell
$env:SECSHARE_VERSION = "1.0.0.3"
irm https://secshare.me/install.ps1 | iex
```

Uninstall:

```powershell
$env:SECSHARE_UNINSTALL = "1"
irm https://secshare.me/install.ps1 | iex
Remove-Item Env:\SECSHARE_UNINSTALL
```

If you installed with a custom directory, pass the same `SECSHARE_INSTALL_DIR` value when uninstalling.

## Supported platforms

| OS | Architecture | Asset |
| --- | --- | --- |
| Linux | x64 | `secshare-linux-x64.tar.gz` |
| Linux | ARM64 | `secshare-linux-arm64.tar.gz` |
| Alpine Linux | x64 | `secshare-linux-musl-x64.tar.gz` |
| Alpine Linux | ARM64 | `secshare-linux-musl-arm64.tar.gz` |
| macOS | Intel x64 | `secshare-osx-x64.tar.gz` |
| macOS | Apple Silicon ARM64 | `secshare-osx-arm64.tar.gz` |
| Windows | x64 | `secshare-win-x64.zip` |
