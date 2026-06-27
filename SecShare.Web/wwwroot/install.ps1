param(
    [ValidateSet("install", "uninstall")]
    [string]$Action = "install",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

$AppName = if ($env:SECSHARE_INSTALL_NAME) { $env:SECSHARE_INSTALL_NAME } else { "secshare" }
$AssetPrefix = if ($env:SECSHARE_ASSET_PREFIX) { $env:SECSHARE_ASSET_PREFIX } else { "secshare" }
$Repo = if ($env:SECSHARE_REPO) { $env:SECSHARE_REPO } else { "lampego/SecShare" }
$Version = if ($env:SECSHARE_VERSION) { $env:SECSHARE_VERSION } else { "latest" }
$InstallDir = if ($env:SECSHARE_INSTALL_DIR) { $env:SECSHARE_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA "Programs\SecShare\bin" }
$SkipChecksum = $env:SECSHARE_SKIP_CHECKSUM -eq "1"

if ($env:SECSHARE_ACTION) {
    if ($env:SECSHARE_ACTION -notin @("install", "uninstall")) {
        throw "Unsupported action: $($env:SECSHARE_ACTION)"
    }

    $Action = $env:SECSHARE_ACTION
}

if ($Uninstall -or $env:SECSHARE_UNINSTALL -match "^(1|true|yes)$") {
    $Action = "uninstall"
}

function Get-ReleaseUrl {
    param([string]$Asset)

    if ($Version -eq "latest") {
        return "https://github.com/$Repo/releases/latest/download/$Asset"
    }

    return "https://github.com/$Repo/releases/download/$Version/$Asset"
}

function Add-UserPath {
    param([string]$PathToAdd)

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $parts = @()

    if ($currentPath) {
        $parts = $currentPath -split ";" | Where-Object { $_ }
    }

    if ($parts -notcontains $PathToAdd) {
        $newPath = if ($currentPath) { "$currentPath;$PathToAdd" } else { $PathToAdd }
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Write-Warning "$PathToAdd was added to the user PATH. Restart the terminal before running $AppName."
    }
}

function Remove-UserPath {
    param([string]$PathToRemove)

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

    if (-not $currentPath) {
        return
    }

    $oldParts = @($currentPath -split ";" | Where-Object { $_ })
    $newParts = @($oldParts | Where-Object { $_ -ne $PathToRemove })

    if ($newParts.Count -ne $oldParts.Count) {
        [Environment]::SetEnvironmentVariable("Path", ($newParts -join ";"), "User")
        Write-Warning "$PathToRemove was removed from the user PATH. Restart the terminal to apply the change."
    }
}

function Uninstall-Binary {
    $target = Join-Path $InstallDir "$AppName.exe"

    if (-not (Test-Path -LiteralPath $target)) {
        Write-Host "$AppName is not installed at $target"
        return
    }

    Remove-Item -LiteralPath $target -Force
    Write-Host "$AppName removed from $target"

    $remainingFiles = Get-ChildItem -LiteralPath $InstallDir -Force -ErrorAction SilentlyContinue | Select-Object -First 1

    if (-not $remainingFiles) {
        Remove-Item -LiteralPath $InstallDir -Force -ErrorAction SilentlyContinue
        Remove-UserPath $InstallDir
    }
}

if ($Action -eq "uninstall") {
    Uninstall-Binary
    return
}

if (-not [Environment]::Is64BitOperatingSystem) {
    throw "Only 64-bit Windows is supported by the current release assets."
}

if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") {
    throw "Windows ARM64 is not supported by the current release assets. Add win-arm64 to the release to support it."
}

$Rid = "win-x64"
$Asset = "$AssetPrefix-$Rid.zip"
$TempDir = Join-Path ([IO.Path]::GetTempPath()) ("secshare-install-" + [Guid]::NewGuid().ToString("N"))
$ArchivePath = Join-Path $TempDir $Asset
$ChecksumsPath = Join-Path $TempDir "checksums.txt"

New-Item -ItemType Directory -Path $TempDir | Out-Null

try {
    $Url = Get-ReleaseUrl $Asset
    Write-Host "Downloading $Asset..."
    Invoke-WebRequest -Uri $Url -OutFile $ArchivePath

    if (-not $SkipChecksum) {
        try {
            Invoke-WebRequest -Uri (Get-ReleaseUrl "checksums.txt") -OutFile $ChecksumsPath
            $expected = Get-Content $ChecksumsPath |
                ForEach-Object {
                    $parts = $_ -split "\s+"
                    if ($parts.Count -ge 2 -and [IO.Path]::GetFileName($parts[1]) -eq $Asset) {
                        $parts[0]
                    }
                } |
                Select-Object -First 1

            if ($expected) {
                $actual = (Get-FileHash -Algorithm SHA256 $ArchivePath).Hash.ToLowerInvariant()
                if ($actual -ne $expected.ToLowerInvariant()) {
                    throw "Checksum verification failed for $Asset"
                }

                Write-Host "Checksum verified."
            } else {
                Write-Warning "$Asset is not present in checksums.txt; checksum verification skipped."
            }
        } catch {
            Write-Warning "Checksum verification skipped: $($_.Exception.Message)"
        }
    } else {
        Write-Warning "Checksum verification skipped by SECSHARE_SKIP_CHECKSUM=1."
    }

    Expand-Archive -Path $ArchivePath -DestinationPath $TempDir -Force

    $candidates = @("$AppName.exe", "secshare.exe", "SecShare.Console.exe")
    $binary = $null

    foreach ($candidate in $candidates) {
        $match = Get-ChildItem -Path $TempDir -Recurse -File -Filter $candidate | Select-Object -First 1
        if ($match) {
            $binary = $match
            break
        }
    }

    if (-not $binary) {
        throw "Release archive does not contain a supported CLI binary."
    }

    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    $target = Join-Path $InstallDir "$AppName.exe"
    Copy-Item -Path $binary.FullName -Destination $target -Force

    Add-UserPath $InstallDir
    Write-Host "$AppName installed to $target"
    Write-Host "Run: $AppName --help"
} finally {
    Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}
