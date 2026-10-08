param([Parameter(Mandatory=$true)][string]$Archive)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $repoRoot ('build\package-test-' + [Guid]::NewGuid().ToString('N'))
$source = Join-Path $testRoot 'extracted'
$destination = Join-Path $testRoot ('custom folder ' + [char]0x4E2D + [char]0x6587)
$shortcuts = Join-Path $testRoot 'shortcuts'
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::ExtractToDirectory([IO.Path]::GetFullPath($Archive),$source)
$checks = @()
$files = @(Get-ChildItem -LiteralPath $source -File)
if ($files.Count -ne 10 -or (Test-Path -LiteralPath (Join-Path $source 'data'))) { throw 'Unexpected package contents.' }
$checks += 'Package contains all ten public files and no personal data'
$installer = Join-Path $source 'Install.ps1'
$bytes = [IO.File]::ReadAllBytes($installer)
if ($bytes[0] -ne 239 -or $bytes[1] -ne 187 -or $bytes[2] -ne 191) { throw 'Installer needs UTF-8 BOM for Windows PowerShell.' }
$checks += 'Installer encoding supports Windows PowerShell 5.1'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -Destination $destination -ShortcutDirectory $shortcuts -NoLaunch
if ($LASTEXITCODE -ne 0) { throw 'Custom-path installation failed.' }
foreach ($file in $files) {
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $destination $file.Name)).Hash) { throw ('Installed file differs: ' + $file.Name) }
}
$checks += 'Installer copies validated files to a custom path with spaces and Unicode'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path $shortcuts 'Token Monitor.lnk'))
if ($shortcut.TargetPath -ne (Join-Path $destination 'TokenMonitor.exe') -or $shortcut.WorkingDirectory -ne $destination) { throw 'Shortcut target is incorrect.' }
$checks += 'Shortcut points to installed executable and working directory'
$legacy = Join-Path $destination 'data'
New-Item -ItemType Directory -Path $legacy | Out-Null
[IO.File]::WriteAllText((Join-Path $legacy 'preserve-test.txt'),'legacy-data-must-remain')
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -Destination $destination -ShortcutDirectory $shortcuts -NoLaunch
if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText((Join-Path $legacy 'preserve-test.txt')) -ne 'legacy-data-must-remain') { throw 'In-place reinstall did not preserve legacy data.' }
$checks += 'In-place reinstall preserves existing data'
$readme = Join-Path $source 'START_HERE.zh-CN.md'
[IO.File]::AppendAllText($readme,'checksum-test-change')
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -Destination (Join-Path $testRoot 'must-not-install') -ShortcutDirectory $shortcuts -NoLaunch
if ($LASTEXITCODE -eq 0 -or (Test-Path -LiteralPath (Join-Path $testRoot 'must-not-install'))) { throw 'Damaged package was not rejected before installation.' }
$checks += 'Damaged package is rejected before destination creation'
$checks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $testRoot 'checks.json') -Encoding UTF8
Write-Output ('Package checks passed: ' + $checks.Count + '. No account queries performed.')
Write-Output ('Evidence: ' + $testRoot)
