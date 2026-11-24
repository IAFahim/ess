$ErrorActionPreference = "Stop"

$repo = "IAFahim/ess"
$baseUrl = if ($env:ESS_BASE_URL) { $env:ESS_BASE_URL } else { "https://github.com/$repo/releases/latest/download" }
$installDir = if ($env:ESS_INSTALL_DIR) { $env:ESS_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA "ess" }
$architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "arm64" } else { "x64" }
$asset = "ess-win-$architecture.zip"

$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $workDir | Out-Null

try {
    $archive = Join-Path $workDir $asset
    try {
        Invoke-WebRequest -Uri "$baseUrl/$asset" -OutFile $archive
    } catch {
        if (Get-Command gh -ErrorAction SilentlyContinue) {
            gh release download --repo $repo --pattern $asset --dir $workDir
        } else {
            throw "Could not download $asset. If the repo is private, install gh and run: gh auth login"
        }
    }

    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
    Expand-Archive -Path $archive -DestinationPath $installDir -Force

    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if (($userPath -split ";") -notcontains $installDir) {
        [Environment]::SetEnvironmentVariable("Path", "$userPath;$installDir", "User")
        Write-Host "Added $installDir to your PATH. Open a new terminal to use ess."
    }

    Write-Host "Installed ess to $installDir"

    Write-Host ""
    Write-Host "What ess needs:"
    if (Get-Command git -ErrorAction SilentlyContinue) { Write-Host "  ok  git" } else { Write-Host "  !   git is missing. Get it from https://git-scm.com/downloads" }
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        gh auth status *> $null
        if ($LASTEXITCODE -eq 0) { Write-Host "  ok  gh, logged in" } else { Write-Host "  !   gh is not logged in. Run:  gh auth login" }
    } else {
        Write-Host "  !   gh is missing (only needed to put folders on GitHub). Get it from https://cli.github.com"
    }
    Write-Host ""
    Write-Host "Start:  mkdir door; cd door; ess n"
} finally {
    Remove-Item -Recurse -Force $workDir
}
