param([string]$OutputPath)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
if(-not $OutputPath){$OutputPath=Join-Path $repoRoot 'assets\readme-hero.png'}
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Net.Http,System.Windows.Forms
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repoRoot 'build\TokenMonitor.exe'))
$app=[TokenMonitor.MonitorApp]::new('readme-preview',$true)
$stream=$assembly.GetManifestResourceStream('TokenMonitor.Theme.xaml')
try{$app.Resources.MergedDictionaries.Add([Windows.Markup.XamlReader]::Load($stream))}finally{$stream.Dispose()}
$app.Prefs=[TokenMonitor.Preferences]::new()
$app.Prefs.Appearance='dark';$app.Prefs.FontSize='large';$app.Prefs.OpenAi=$true
[TokenMonitor.Appearance]::Apply('dark');[TokenMonitor.FontSizing]::Apply('large')
$flags=[Reflection.BindingFlags]'Static,NonPublic'
$app.GptData=[TokenMonitor.MonitorApp].GetMethod('SampleGpt',$flags).Invoke($null,@())
$app.DeepData=[TokenMonitor.MonitorApp].GetMethod('SampleDeep',$flags).Invoke($null,@())
$app.OpenAiData=[TokenMonitor.MonitorApp].GetMethod('SampleOpenAi',$flags).Invoke($null,@())
$at=[DateTimeOffset]::Parse('2026-10-26T12:00:00+08:00')
$app.GptData.Fetched=$at;$app.DeepData.Fetched=$at;$app.OpenAiData.Fetched=$at
$app.GptData.Groups[0].Windows[0].Reset=$at.AddHours(3)
$app.GptData.Groups[0].Windows[1].Reset=$at.AddDays(4)
$main=[TokenMonitor.MainView]::new($app);$app.Main=$main
$main.GptInfo.Text='已同步 12:00:00 · 剩余额度';$main.DeepInfo.Text='已同步 12:00:00 · API';$main.OpenAiInfo.Text='历史用量已连接 · 12:00:00'
$main.Surface.Measure([Windows.Size]::new(1180,760));$main.Surface.Arrange([Windows.Rect]::new(0,0,1180,760));$main.Surface.UpdateLayout()
$orb=[TokenMonitor.OrbView]::new($app);$app.Orb=$orb;$orb.GetType().GetMethod('Render').Invoke($orb,[object[]]@($app.GptData,$null,$false))
$orb.Content.Measure([Windows.Size]::new(108,108));$orb.Content.Arrange([Windows.Rect]::new(0,0,108,108));$orb.Content.UpdateLayout()
# Render native WPF controls together at 2x density. No account queries or bitmap upscaling.
$visual=[Windows.Media.DrawingVisual]::new();$dc=$visual.RenderOpen()
$dc.DrawRectangle([Windows.Media.VisualBrush]::new($main.Surface),$null,[Windows.Rect]::new(0,0,1180,760))
$dc.DrawRectangle([Windows.Media.VisualBrush]::new($orb.Content),$null,[Windows.Rect]::new(1040,622,108,108))
$dc.Close()
$bitmap=[Windows.Media.Imaging.RenderTargetBitmap]::new(2360,1520,192,192,[Windows.Media.PixelFormats]::Pbgra32)
$bitmap.Render($visual)
$encoder=[Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$file=[IO.File]::Create($OutputPath);try{$encoder.Save($file)}finally{$file.Dispose()}
$main.Exiting=$true;$orb.Exiting=$true;$app.Shutdown()
Write-Output ('README image: '+$OutputPath+' (2360 x 1520, native 2x render, mock data)')

