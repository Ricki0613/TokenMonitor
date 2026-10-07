param([string]$BuildPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $repoRoot 'build' }
$BuildPath = [IO.Path]::GetFullPath($BuildPath)
$executable = Join-Path $BuildPath 'TokenMonitor.exe'
$previewFolder = Join-Path $BuildPath ('ui-test-' + [Guid]::NewGuid().ToString('N'))
$process = Start-Process -FilePath $executable -ArgumentList @('--ui-test', ('"' + $previewFolder + '"')) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'UI checks timed out.' }
$report = Join-Path $previewFolder 'smoke.json'
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $report)) { throw 'UI checks did not complete.' }
$result = Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
$checks = @($result.PSObject.Properties | Where-Object { $_.Value -is [bool] })
$failed = @($checks | Where-Object { -not $_.Value })
if ($failed.Count -ne 0) { throw ('UI checks failed: ' + ($failed.Name -join ', ')) }
Write-Output ('UI checks passed: ' + $checks.Count + '. Mock data only; no account queries or reset credits consumed.')
Write-Output ('Screenshots: ' + $previewFolder)
