param(
    [string]$Destination,
    [switch]$Interactive,
    [switch]$NoLaunch,
    [string]$ShortcutDirectory
)
$ErrorActionPreference = 'Stop'
try {
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Token Monitor requires 64-bit Windows 10 or 11.' }
    $framework = Get-ItemProperty -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue
    if (-not $framework -or $framework.Release -lt 528040) { throw 'Please install .NET Framework 4.8, then run Install.cmd again. See START_HERE.zh-CN.md.' }
    if (-not $Destination) { $Destination = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\TokenMonitor' }
    if ($Interactive) {
        Write-Host 'Token Monitor v1.1.1'
        Write-Host '安装程序和创建快捷方式。日常运行不需要管理员权限。'
        Write-Host ('默认安装位置：' + $Destination)
        $chosen = Read-Host '按 Enter 使用默认位置，或输入自定义完整路径'
        if (-not [string]::IsNullOrWhiteSpace($chosen)) { $Destination = $chosen.Trim().Trim('"') }
    }
    $Destination = [Environment]::ExpandEnvironmentVariables($Destination)
    if (-not [IO.Path]::IsPathRooted($Destination)) { throw 'Please provide an absolute installation path.' }
    $Destination = [IO.Path]::GetFullPath($Destination)
    $files = @('TokenMonitor.exe','TokenMonitor.exe.config','TokenMonitor.ico','START_HERE.zh-CN.md','USER_GUIDE.zh-CN.md','RELEASE_NOTES.zh-CN.md','LICENSE','Install.cmd','Install.ps1','SHA256SUMS.txt')
    foreach ($name in $files) { if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $name) -PathType Leaf)) { throw ('Package incomplete: ' + $name + '. Extract the entire ZIP before installing.') } }
    $verified = @{}
    foreach ($line in [IO.File]::ReadAllLines((Join-Path $PSScriptRoot 'SHA256SUMS.txt'))) {
        if ($line -notmatch '^([0-9a-fA-F]{64})  (.+)$') { throw 'Package checksum list is invalid. Download the ZIP again.' }
        $expected = $Matches[1]; $name = $Matches[2]
        if ($files -notcontains $name -or $name -eq 'SHA256SUMS.txt' -or $verified.ContainsKey($name)) { throw 'Unexpected package checksum entry.' }
        if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $name) -Algorithm SHA256).Hash -ne $expected) { throw ('Package file is damaged: ' + $name + '. Download and extract the ZIP again.') }
        $verified[$name] = $true
    }
    if ($verified.Count -ne $files.Count - 1) { throw 'Package checksum list is incomplete.' }
    $targetExe = Join-Path $Destination 'TokenMonitor.exe'
    $running = @(Get-Process TokenMonitor -ErrorAction SilentlyContinue | Where-Object { -not $NoLaunch -or $_.Path -eq $targetExe })
    if ($running.Count -gt 0) { throw '请先从 Token Monitor 托盘菜单选择“退出”，再重新运行安装助手。' }
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    $sameFolder = [string]::Equals($PSScriptRoot.TrimEnd('\'),$Destination.TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)
    if (-not $sameFolder) {
        foreach ($name in $files) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $Destination $name) -Force }
    }
    $executable = Join-Path $Destination 'TokenMonitor.exe'
    $shell = New-Object -ComObject WScript.Shell
    $shortcutFolders = if ($ShortcutDirectory) { @([IO.Path]::GetFullPath($ShortcutDirectory)) } else {
        @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('Programs'))
    }
    $shortcutWarnings = @()
    foreach ($folder in $shortcutFolders) {
        try {
            New-Item -ItemType Directory -Path $folder -Force | Out-Null
            $shortcut = $shell.CreateShortcut((Join-Path $folder 'Token Monitor.lnk'))
            $shortcut.TargetPath = $executable
            $shortcut.WorkingDirectory = $Destination
            $shortcut.IconLocation = (Join-Path $Destination 'TokenMonitor.ico') + ',0'
            $shortcut.Description = 'Token Monitor - GPT / DeepSeek'
            $shortcut.Save()
        } catch { $shortcutWarnings += $folder }
    }
    Write-Host ('安装完成：' + $executable)
    Write-Host ('用户数据自动保存在：' + (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'TokenMonitor'))
    if ($shortcutWarnings.Count -gt 0) { Write-Warning ('程序已安装，但快捷方式未能创建到：' + ($shortcutWarnings -join ', ') + '。可直接运行安装目录中的 TokenMonitor.exe。') }
    if (-not $NoLaunch) { Start-Process -FilePath $executable -WorkingDirectory $Destination -WindowStyle Hidden }
    if ($Interactive) { Read-Host '按 Enter 关闭安装助手' | Out-Null }
} catch {
    Write-Host ('安装未完成：' + $_.Exception.Message) -ForegroundColor Red
    Write-Host '若目标目录拒绝写入，请选择自己可写的位置，或直接运行完整解压后的 TokenMonitor.exe。配置保存始终使用 Windows 用户数据目录。'
    exit 1
}
