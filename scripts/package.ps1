param([string]$Version = '2.0.0', [string]$BuildPath, [string]$OutputPath)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must be major.minor.patch.' }
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $repoRoot 'build' }
$BuildPath = [IO.Path]::GetFullPath($BuildPath)
if (-not $OutputPath) { $OutputPath = Join-Path $repoRoot 'dist' }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$executable = Join-Path $BuildPath 'TokenMonitor.exe'
if ((Get-Item -LiteralPath $executable).VersionInfo.FileVersion -ne ($Version + '.0')) { throw 'Build version does not match package version.' }
$stage = Join-Path $BuildPath ('package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage,$OutputPath -Force | Out-Null
foreach ($name in @('TokenMonitor.exe','TokenMonitor.exe.config','TokenMonitor.ico')) { Copy-Item -LiteralPath (Join-Path $BuildPath $name) -Destination $stage }
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\USER_GUIDE.zh-CN.md') -Destination (Join-Path $stage 'USER_GUIDE.zh-CN.md')
Copy-Item -LiteralPath (Join-Path $repoRoot ('docs\RELEASE_v' + $Version + '.md')) -Destination (Join-Path $stage 'RELEASE_NOTES.zh-CN.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\START_HERE.zh-CN.md') -Destination $stage
Copy-Item -LiteralPath (Join-Path $repoRoot 'installer\Install.cmd') -Destination $stage
# Windows PowerShell 5.1 needs a UTF-8 BOM for Chinese text in script files.
$installer = [IO.File]::ReadAllText((Join-Path $repoRoot 'installer\Install.ps1'))
[IO.File]::WriteAllText((Join-Path $stage 'Install.ps1'),$installer,[Text.UTF8Encoding]::new($true))
$lines = @(Get-ChildItem -LiteralPath $stage -File | Sort-Object Name | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name })
[IO.File]::WriteAllLines((Join-Path $stage 'SHA256SUMS.txt'),$lines,[Text.UTF8Encoding]::new($false))
$archive = Join-Path $OutputPath ('TokenMonitor-v' + $Version + '-windows-x64.zip')
if (Test-Path -LiteralPath $archive) { throw 'Package already exists; preserve it or choose another output folder.' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$archive,[IO.Compression.CompressionLevel]::Optimal,$false)
Write-Output ('Package: ' + $archive)
Write-Output ('SHA-256: ' + (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant())
