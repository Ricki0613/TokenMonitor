<img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">

# Token Monitor

**Keep Codex / ChatGPT Work usage limits and your DeepSeek API balance visible on your Windows desktop.**

[Download for Windows](https://github.com/Ricki0613/TokenMonitor/releases/latest/download/TokenMonitor-v1.1.0-windows-x64.zip) · [Report an issue](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) · [中文](README.md)

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

1. Download the Windows x64 ZIP from [Releases](https://github.com/Ricki0613/TokenMonitor/releases/latest) and extract it to a writable folder.
2. Run `TokenMonitor.exe`.
3. For GPT usage, install Codex and sign in locally. For DeepSeek, enter your own API Key in the app's settings (设置).
4. Enable the windows you want with the two switches.

Requires Windows 10 / 11 x64 and .NET Framework 4.8. The application UI is currently Chinese; reset times use Beijing time (UTC+8). Python, Node and the .NET SDK are not required to run the app.

The GPT window reports **Codex / ChatGPT Work subscription quotas**, not every ChatGPT chat model's message limit. The DeepSeek window reports **account balances**, not historical account-wide token counts. The app does not send model inference requests to check usage.

## Build and test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

Output: `build/`. The 21 offline self-tests and 35 UI checks use mock data, require no real accounts or API keys, and never consume reset credits. UI screenshots are written to `build/ui-test-*/`. Runtime data and credentials are excluded from Git.

The release is unsigned. Automatic startup and automatic updates are not implemented. Service-side updates or API changes may affect availability.

## Contribute

Issues and pull requests are welcome. Include reproduction steps or the use case, and keep credentials and account data out of reports. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE). An independent project, not affiliated with OpenAI or DeepSeek.
