# Installing SecShare CLI

The release assets are published from `lampego/SecShare` and currently use the `devshare-*` asset prefix:

- `devshare-linux-x64.tar.gz`
- `devshare-linux-arm64.tar.gz`
- `devshare-osx-x64.tar.gz`
- `devshare-osx-arm64.tar.gz`
- `devshare-win-x64.zip`
- `checksums.txt`

The installer installs the command as `devshare` by default. Override it with `SECSHARE_INSTALL_NAME=secshare` if the public command should be `secshare`.

## Linux and macOS

```bash
curl -fsSL https://raw.githubusercontent.com/lampego/SecShare/main/install.sh | sh
```

Install a specific release:

```bash
curl -fsSL https://raw.githubusercontent.com/lampego/SecShare/main/install.sh | SECSHARE_VERSION=1.0.0.3 sh
```

Install without sudo:

```bash
curl -fsSL https://raw.githubusercontent.com/lampego/SecShare/main/install.sh | SECSHARE_INSTALL_DIR="$HOME/.local/bin" sh
```

Install as `secshare` instead of `devshare`:

```bash
curl -fsSL https://raw.githubusercontent.com/lampego/SecShare/main/install.sh | SECSHARE_INSTALL_NAME=secshare sh
```

## Windows

```powershell
irm https://raw.githubusercontent.com/lampego/SecShare/main/install.ps1 | iex
```

Install a specific release:

```powershell
$env:SECSHARE_VERSION = "1.0.0.3"
irm https://raw.githubusercontent.com/lampego/SecShare/main/install.ps1 | iex
```

## Supported platforms

| OS | Architecture | Asset |
| --- | --- | --- |
| Linux | x64 | `devshare-linux-x64.tar.gz` |
| Linux | ARM64 | `devshare-linux-arm64.tar.gz` |
| macOS | Intel x64 | `devshare-osx-x64.tar.gz` |
| macOS | Apple Silicon ARM64 | `devshare-osx-arm64.tar.gz` |
| Windows | x64 | `devshare-win-x64.zip` |

Alpine Linux is not supported by the current release assets. Add `linux-musl-x64` and `linux-musl-arm64` artifacts if Alpine support is needed.
