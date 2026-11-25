# Stream Extra - one-click publish.
# 1) asks for the new version (Enter = next number)   2) writes version + today's date into app\package.json
# 3) builds StreamExtra-Setup.exe                       4) prepares the folder "GitHub_Upload" (ready to drag into GitHub)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$inv  = [Globalization.CultureInfo]::InvariantCulture
$utf8 = New-Object System.Text.UTF8Encoding($false)      # UTF-8 WITHOUT BOM (a BOM would break the update check)

try {
  Write-Host ''
  Write-Host '==========================================='
  Write-Host '      Stream Extra - Publish Update'
  Write-Host '==========================================='

  # ---------- 1) version ----------
  $pkgPath = Join-Path $root 'app\package.json'
  $pkg = [IO.File]::ReadAllText($pkgPath)
  if ($pkg -notmatch '"version"\s*:\s*"([^"]+)"') { throw 'Could not find "version" in app\package.json' }
  $cur = $Matches[1]
  $parts = $cur.Split('.'); $n = 0
  if ([int]::TryParse($parts[$parts.Length - 1], [ref]$n)) { $parts[$parts.Length - 1] = [string]($n + 1); $next = ($parts -join '.') } else { $next = "$cur.1" }
  Write-Host ''
  Write-Host "Current version : $cur"
  $in = Read-Host "New version (press Enter for $next)"
  if ([string]::IsNullOrWhiteSpace($in)) { $in = $next }
  $in = $in.Trim()
  if ($in -notmatch '^\d+(\.\d+)*$') { throw "Bad version '$in' - use numbers and dots only, for example 1.2" }
  if ($in -eq $cur) {
    Write-Host 'This is the SAME version as now - users will NOT see an update.' -ForegroundColor Yellow
    $a = Read-Host 'Continue anyway? (y/N)'
    if ($a -notmatch '^[yY]') { throw 'Cancelled.' }
  }
  $today = (Get-Date).ToString('yyyy-MM-dd', $inv)

  $pkg = [regex]::Replace($pkg, '"version"\s*:\s*"[^"]*"', ('"version": "' + $in + '"'))
  if ($pkg -match '"releaseDate"') { $pkg = [regex]::Replace($pkg, '"releaseDate"\s*:\s*"[^"]*"', ('"releaseDate": "' + $today + '"')) }
  else { $pkg = [regex]::Replace($pkg, '("version"\s*:\s*"[^"]*")', ('$1,' + "`r`n" + '  "releaseDate": "' + $today + '"')) }
  $null = $pkg | ConvertFrom-Json                         # make sure the JSON is still valid
  [IO.File]::WriteAllText($pkgPath, $pkg, $utf8)
  Write-Host "  -> version $in  ($today)" -ForegroundColor Green

  # ---------- 2) Setup.exe ----------
  Write-Host ''
  Write-Host 'Building StreamExtra-Setup.exe ...'
  $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
  if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
  if (-not (Test-Path $csc)) { throw 'csc.exe not found. Install .NET Framework 4.x (it ships with Windows 10/11).' }
  $rel = Join-Path $root 'release'
  New-Item -ItemType Directory -Force -Path $rel | Out-Null
  $setupExe = Join-Path $rel 'StreamExtra-Setup.exe'
  & $csc /nologo /target:winexe /optimize+ /codepage:65001 "/out:$setupExe" "/win32icon:app\assets\icon.ico" `
    /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll setup\Setup.cs
  if ($LASTEXITCODE -ne 0 -or -not (Test-Path $setupExe)) { throw 'Setup build failed.' }

  # ---------- 3) GitHub_Upload folder ----------
  Write-Host ''
  Write-Host 'Preparing the GitHub_Upload folder ...'
  $out = Join-Path $root 'GitHub_Upload'
  if (Test-Path $out) { Remove-Item $out -Recurse -Force }
  New-Item -ItemType Directory -Force -Path $out | Out-Null
  Copy-Item (Join-Path $root 'app')   -Destination $out -Recurse -Force
  Copy-Item (Join-Path $root 'setup') -Destination $out -Recurse -Force
  foreach ($f in 'README.txt', 'build.ps1', 'Build_StreamExtra.cmd', 'Publish_Update.cmd', 'publish.ps1') {
    $p = Join-Path $root $f; if (Test-Path $p) { Copy-Item $p -Destination $out -Force }
  }
  Copy-Item $setupExe -Destination $out -Force

  # repo name is read from setup\Setup.cs (Cfg.Repo) so it only lives in one place
  $repo = 'b9storeb9-coder/StreamExtra'
  $cs = [IO.File]::ReadAllText((Join-Path $root 'setup\Setup.cs'))
  if ($cs -match 'Repo\s*=\s*"([^"]+)"') { $repo = $Matches[1] }
  $url = "https://github.com/$repo/upload/main"

  Write-Host ''
  Write-Host '==========================================='
  Write-Host "  READY  -  version $in"
  Write-Host '==========================================='
  Write-Host ''
  Write-Host '  Now, in the GitHub page that just opened:'
  Write-Host '   1) In the folder GitHub_Upload: press Ctrl+A (select everything inside it)'
  Write-Host '   2) Drag it into the GitHub page and wait for the upload to finish'
  Write-Host '   3) Press the green "Commit changes" button'
  Write-Host ''
  Write-Host '  That is all. Everyone who opens the app will get the Update page.'
  Write-Host "  (Page: $url)"
  Start-Process explorer.exe $out
  Start-Process $url
  exit 0
}
catch {
  Write-Host ''
  Write-Host "[X] $($_.Exception.Message)" -ForegroundColor Red
  exit 1
}
