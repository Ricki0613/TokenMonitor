param([string]$BuildPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $BuildPath) { $BuildPath = Join-Path $repoRoot 'build' }
$BuildPath = [IO.Path]::GetFullPath($BuildPath)
$executable = Join-Path $BuildPath 'TokenMonitor.exe'
$report = Join-Path $BuildPath 'self-test.json'
if (-not (Test-Path -LiteralPath $executable)) { throw 'Build the project before running tests.' }
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
$process = Start-Process -FilePath $executable -ArgumentList @('--self-test', ('"' + $report + '"')) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'Self-tests timed out.' }
if (-not (Test-Path -LiteralPath $report)) { throw 'Self-test report was not created.' }
$result = Get-Content -LiteralPath $report -Raw -Encoding UTF8 | ConvertFrom-Json
if ($process.ExitCode -ne 0 -or @($result.failed).Count -ne 0) { throw ('Self-tests failed: ' + ($result.failed -join '; ')) }
Write-Output ('Passed: ' + @($result.passed).Count + ' / Failed: 0. No live account queries performed.')
