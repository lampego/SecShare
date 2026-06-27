#!/usr/bin/env sh
set -eu

APP_NAME="${SECSHARE_INSTALL_NAME:-secshare}"
ASSET_PREFIX="${SECSHARE_ASSET_PREFIX:-secshare}"
REPO="${SECSHARE_REPO:-lampego/SecShare}"
VERSION="${SECSHARE_VERSION:-latest}"
INSTALL_DIR="${SECSHARE_INSTALL_DIR:-}"
SKIP_CHECKSUM="${SECSHARE_SKIP_CHECKSUM:-0}"
BINARY_CANDIDATES="${SECSHARE_BINARY_CANDIDATES:-$APP_NAME secshare SecShare.Console}"
ACTION="${SECSHARE_ACTION:-install}"

case "${SECSHARE_UNINSTALL:-0}" in
  1|true|TRUE|yes|YES)
    ACTION="uninstall"
    ;;
esac

say() {
  printf '%s\n' "$*"
}

warn() {
  printf '%s\n' "warning: $*" >&2
}

die() {
  printf '%s\n' "secshare install: $*" >&2
  exit 1
}

has() {
  command -v "$1" >/dev/null 2>&1
}

download_to() {
  url="$1"
  dest="$2"

  if has curl; then
    curl -fsSL "$url" -o "$dest"
  elif has wget; then
    wget -q "$url" -O "$dest"
  else
    die "curl or wget is required"
  fi
}

detect_rid() {
  os_raw="$(uname -s 2>/dev/null || true)"
  arch_raw="$(uname -m 2>/dev/null || true)"

  case "$arch_raw" in
    x86_64|amd64)
      arch="x64"
      ;;
    arm64|aarch64)
      arch="arm64"
      ;;
    *)
      die "unsupported CPU architecture: $arch_raw"
      ;;
  esac

  case "$os_raw" in
    Linux)
      if is_musl_linux "$arch"; then
        RID="linux-musl-$arch"
      else
        RID="linux-$arch"
      fi
      ;;
    Darwin)
      RID="osx-$arch"
      ;;
    *)
      die "unsupported OS: $os_raw. Use install.ps1 on Windows."
      ;;
  esac
}

is_musl_linux() {
  target_arch="$1"

  if [ -f /etc/alpine-release ]; then
    return 0
  fi

  if has ldd && ldd /bin/sh 2>&1 | grep -qi musl; then
    return 0
  fi

  case "$target_arch" in
    x64)
      [ -e /lib/ld-musl-x86_64.so.1 ] && return 0
      ;;
    arm64)
      [ -e /lib/ld-musl-aarch64.so.1 ] && return 0
      ;;
  esac

  return 1
}

release_url_for() {
  asset="$1"

  if [ "$VERSION" = "latest" ]; then
    printf 'https://github.com/%s/releases/latest/download/%s' "$REPO" "$asset"
  else
    printf 'https://github.com/%s/releases/download/%s/%s' "$REPO" "$VERSION" "$asset"
  fi
}

sha256_file() {
  file="$1"

  if has sha256sum; then
    sha256sum "$file" | awk '{print $1}'
  elif has shasum; then
    shasum -a 256 "$file" | awk '{print $1}'
  else
    return 1
  fi
}

verify_checksum() {
  asset="$1"
  file="$2"

  if [ "$SKIP_CHECKSUM" = "1" ]; then
    warn "checksum verification skipped by SECSHARE_SKIP_CHECKSUM=1"
    return 0
  fi

  if ! has awk; then
    warn "awk is not available; checksum verification skipped"
    return 0
  fi

  checksums_file="$TMP_DIR/checksums.txt"
  checksums_url="$(release_url_for checksums.txt)"

  if ! download_to "$checksums_url" "$checksums_file"; then
    warn "checksums.txt was not found in the release; checksum verification skipped"
    return 0
  fi

  expected_hash="$(awk -v f="$asset" '
    {
      name = $2
      sub(/^.*\//, "", name)
      if (name == f) {
        print $1
        exit
      }
    }
  ' "$checksums_file")"

  if [ -z "$expected_hash" ]; then
    warn "$asset is not present in checksums.txt; checksum verification skipped"
    return 0
  fi

  if ! actual_hash="$(sha256_file "$file")"; then
    warn "sha256sum/shasum is not available; checksum verification skipped"
    return 0
  fi

  if [ "$actual_hash" != "$expected_hash" ]; then
    die "checksum verification failed for $asset"
  fi

  say "Checksum verified."
}

find_binary() {
  for candidate in $BINARY_CANDIDATES; do
    if [ -f "$TMP_DIR/$candidate" ]; then
      BINARY_PATH="$TMP_DIR/$candidate"
      return 0
    fi
  done

  BINARY_PATH="$(find "$TMP_DIR" -type f \( -name "$APP_NAME" -o -name secshare -o -name SecShare.Console \) 2>/dev/null | head -n 1 || true)"

  if [ -n "$BINARY_PATH" ] && [ -f "$BINARY_PATH" ]; then
    return 0
  fi

  die "release archive does not contain a supported CLI binary"
}

default_install_dir() {
  if [ -n "$INSTALL_DIR" ]; then
    return 0
  fi

  if [ "$(id -u)" -eq 0 ]; then
    INSTALL_DIR="/usr/local/bin"
  elif [ -d /usr/local/bin ] && [ -w /usr/local/bin ]; then
    INSTALL_DIR="/usr/local/bin"
  elif has sudo; then
    INSTALL_DIR="/usr/local/bin"
  else
    [ -n "${HOME:-}" ] || die "HOME is not set; set SECSHARE_INSTALL_DIR"
    INSTALL_DIR="$HOME/.local/bin"
  fi
}

install_binary() {
  target="$INSTALL_DIR/$APP_NAME"

  if [ -d "$INSTALL_DIR" ] && [ -w "$INSTALL_DIR" ]; then
    cp "$BINARY_PATH" "$target"
    chmod 755 "$target"
    return 0
  fi

  if [ ! -e "$INSTALL_DIR" ] && mkdir -p "$INSTALL_DIR" 2>/dev/null; then
    cp "$BINARY_PATH" "$target"
    chmod 755 "$target"
    return 0
  fi

  if has sudo; then
    sudo mkdir -p "$INSTALL_DIR"
    sudo cp "$BINARY_PATH" "$target"
    sudo chmod 755 "$target"
    return 0
  fi

  die "cannot write to $INSTALL_DIR and sudo is not available; set SECSHARE_INSTALL_DIR=\$HOME/.local/bin"
}

uninstall_binary() {
  target="$INSTALL_DIR/$APP_NAME"

  if [ ! -e "$target" ]; then
    say "$APP_NAME is not installed at $target"
    return 0
  fi

  if [ -d "$INSTALL_DIR" ] && [ -w "$INSTALL_DIR" ]; then
    rm -f "$target"
    say "$APP_NAME removed from $target"
    return 0
  fi

  if has sudo; then
    sudo rm -f "$target"
    say "$APP_NAME removed from $target"
    return 0
  fi

  die "cannot remove $target and sudo is not available"
}

if [ "$#" -gt 0 ]; then
  case "$1" in
    install|uninstall)
      ACTION="$1"
      ;;
    -h|--help|help)
      say "Usage: install.sh [install|uninstall]"
      say "Environment: SECSHARE_INSTALL_NAME, SECSHARE_INSTALL_DIR, SECSHARE_VERSION, SECSHARE_REPO"
      exit 0
      ;;
    *)
      die "unsupported action: $1"
      ;;
  esac
fi

case "$ACTION" in
  install|uninstall)
    ;;
  *)
    die "unsupported action: $ACTION"
    ;;
esac

if [ "$ACTION" = "uninstall" ]; then
  default_install_dir
  uninstall_binary
  exit 0
fi

has tar || die "tar is required"

detect_rid

ASSET="$ASSET_PREFIX-$RID.tar.gz"
URL="$(release_url_for "$ASSET")"
TMP_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t secshare)"
BINARY_PATH=""
trap 'rm -rf "$TMP_DIR"' EXIT HUP INT TERM

say "Downloading $ASSET..."
download_to "$URL" "$TMP_DIR/$ASSET"
verify_checksum "$ASSET" "$TMP_DIR/$ASSET"

tar -xzf "$TMP_DIR/$ASSET" -C "$TMP_DIR"
find_binary
chmod +x "$BINARY_PATH"

default_install_dir
install_binary

say "$APP_NAME installed to $INSTALL_DIR/$APP_NAME"

case ":$PATH:" in
  *":$INSTALL_DIR:"*)
    ;;
  *)
    warn "$INSTALL_DIR is not in PATH. Add it with: export PATH=\"$INSTALL_DIR:\$PATH\""
    ;;
esac

say "Run: $APP_NAME --help"
