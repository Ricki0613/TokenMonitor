param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'src'
if (-not $OutputPath) { $OutputPath = Join-Path $repoRoot 'build' }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 x64 compiler is required.' }
$references = @('System.dll','System.Core.dll','System.Xaml.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.Security.dll','System.Windows.Forms.dll','System.Drawing.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$arguments = @('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output',('/out:' + (Join-Path $OutputPath 'TokenMonitor.exe')),('/win32icon:' + (Join-Path $repoRoot 'assets\TokenMonitor.ico')),('/win32manifest:' + (Join-Path $source 'app.manifest')),('/resource:' + (Join-Path $source 'Theme.xaml') + ',TokenMonitor.Theme.xaml'))
foreach ($reference in $references) { $arguments += '/reference:' + (Join-Path $framework $reference) }
$arguments += (Get-ChildItem -LiteralPath $source -Filter '*.cs').FullName
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'C# compilation failed.' }
Copy-Item -LiteralPath (Join-Path $source 'TokenMonitor.exe.config'),(Join-Path $repoRoot 'assets\TokenMonitor.ico') -Destination $OutputPath -Force
Write-Output ('Build succeeded: ' + (Join-Path $OutputPath 'TokenMonitor.exe'))
