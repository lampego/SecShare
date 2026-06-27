# Installing SecShare CLI

The release assets are published from `lampego/SecShare` and currently use the `devshare-*` asset prefix:

- `devshare-linux-x64.tar.gz`
- `devshare-linux-arm64.tar.gz`
- `devshare-linux-musl-x64.tar.gz`
- `devshare-linux-musl-arm64.tar.gz`
- `devshare-osx-x64.tar.gz`
- `devshare-osx-arm64.tar.gz`
- `devshare-win-x64.zip`
- `checksums.txt`

The installer installs the command as `devshare` by default. Override it with `SECSHARE_INSTALL_NAME=secshare` if the public command should be `secshare`.

## Linux and macOS

```bash
curl -fsSL https://sechsare.me/install.sh | sh
```

Install a specific release:

```bash
curl -fsSL https://sechsare.me/install.sh | SECSHARE_VERSION=1.0.0.3 sh
```

Install without sudo:

```bash
curl -fsSL https://sechsare.me/install.sh | SECSHARE_INSTALL_DIR="$HOME/.local/bin" sh
```

Install as `secshare` instead of `devshare`:

```bash
curl -fsSL https://sechsare.me/install.sh | SECSHARE_INSTALL_NAME=secshare sh
```

Uninstall:

```bash
curl -fsSL https://sechsare.me/install.sh | sh -s -- uninstall
```

If you installed with a custom name or directory, pass the same `SECSHARE_INSTALL_NAME` or `SECSHARE_INSTALL_DIR` values when uninstalling.

## Windows

```powershell
irm https://sechsare.me/install.ps1 | iex
```

Install a specific release:

```powershell
$env:SECSHARE_VERSION = "1.0.0.3"
irm https://sechsare.me/install.ps1 | iex
```

Uninstall:

```powershell
$env:SECSHARE_UNINSTALL = "1"
irm https://sechsare.me/install.ps1 | iex
Remove-Item Env:\SECSHARE_UNINSTALL
```

If you installed with a custom name or directory, pass the same `SECSHARE_INSTALL_NAME` or `SECSHARE_INSTALL_DIR` values when uninstalling.

## Supported platforms

| OS | Architecture | Asset |
| --- | --- | --- |
| Linux | x64 | `devshare-linux-x64.tar.gz` |
| Linux | ARM64 | `devshare-linux-arm64.tar.gz` |
| Alpine Linux | x64 | `devshare-linux-musl-x64.tar.gz` |
| Alpine Linux | ARM64 | `devshare-linux-musl-arm64.tar.gz` |
| macOS | Intel x64 | `devshare-osx-x64.tar.gz` |
| macOS | Apple Silicon ARM64 | `devshare-osx-arm64.tar.gz` |
| Windows | x64 | `devshare-win-x64.zip` |
