<img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">

# Token Monitor

**Keep Codex / ChatGPT Work usage limits and your DeepSeek API balance visible on your Windows desktop.**

[Download for Windows](https://github.com/Ricki0613/TokenMonitor/releases/download/v1.1.1/TokenMonitor-v1.1.1-windows-x64.zip) · [Changelog](CHANGELOG.md) · [Report an issue](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) · [中文](README.md)

## Recent releases

| Version | Date | Changes |
| --- | --- | --- |
| [v1.1.1](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.1.1) | 2026-10-08 | Per-user data storage, legacy migration, installation helper, actionable save errors and setup guide |
| [v1.1.0](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.1.0) | 2026-10-07 | Resizable windows, responsive cards, themes, official quota reset and compact height |
| [v1.0.0](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.0.0) | 2026-10-07 | Initial dual-window monitor with refresh, tray and encrypted credentials |

## Features

- Two independent, draggable, resizable, always-on-top windows with separate visibility switches.
- Cards reflow without shrinking text. Default height fits content; the ↕ button restores that height.
- Light, dark and system appearance, with saved window sizes and automatic system-theme updates.
- Redeem an official GPT quota-reset credit after confirmation, with retries that avoid duplicate redemption.
- Codex / ChatGPT Work remaining quota, official time windows, reset times and credits.
- DeepSeek available, topped-up and granted balances, displayed separately for each currency.
- Refresh every 15, 30, 60 or 120 seconds, or refresh manually.
- System tray, saved positions, single-instance launch and clear stale-data indicators.
- DeepSeek credentials encrypted for the current Windows user using DPAPI.

## Quick start

1. Download the Windows x64 ZIP from [Releases](https://github.com/Ricki0613/TokenMonitor/releases/latest) and extract the entire archive.
2. Run `Install.cmd`. Accept the default location or enter your preferred absolute path; the helper creates desktop and Start menu shortcuts. You can also run the extracted `TokenMonitor.exe` directly.
3. For GPT usage, install Codex and sign in locally. For DeepSeek, enter your own API Key in the app's settings (设置).
4. Enable the windows you want with the two switches.

Requires Windows 10 / 11 x64 and .NET Framework 4.8. The application UI is currently Chinese; reset times use Beijing time (UTC+8). Python, Node and the .NET SDK are not required to run the app.

Normal operation and credential saving require no administrator privileges. Settings and encrypted credentials are created automatically in `%LOCALAPPDATA%\TokenMonitor`, independently of the installation directory. Copying program files into a protected directory can still require Windows permission. The ZIP does not need a pre-created `data` folder.

To upgrade v1.0.0/v1.1.0, exit the old app from its tray menu, replace the program files in place and keep the old `data` directory. On startup, valid missing settings and a decryptable key are copied to per-user storage; originals remain and existing per-user data takes precedence. Run once in the old location before relocating the app. See [installation and troubleshooting](docs/START_HERE.zh-CN.md).

The GPT window reports **Codex / ChatGPT Work subscription quotas**, not every ChatGPT chat model's message limit. The DeepSeek window reports **account balances**, not historical account-wide token counts. The app does not send model inference requests to check usage.

## Build and test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

Output: `build/`. The 30 offline self-tests include saving with ACL-denied installation writes, legacy migration and safe error messages. The 35 UI checks use mock data. These tests require no real accounts or API keys and never consume reset credits. UI screenshots are written to `build/ui-test-*/`. Runtime data and credentials are excluded from Git.

The release is unsigned. Automatic startup and automatic updates are not implemented. Service-side updates or API changes may affect availability.

## Contribute

Issues and pull requests are welcome. Include reproduction steps or the use case, and keep credentials and account data out of reports. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE). An independent project, not affiliated with OpenAI or DeepSeek.
