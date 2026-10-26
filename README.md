<p align="center">
  <img src="assets/TokenMonitor.png" width="72" alt="Token Monitor logo">
</p>
<h1 align="center">Token Monitor</h1>
<p align="center"><strong>额度、余额、API 用量，一眼可见。</strong><br>为 Windows 桌面打造的用量监控工具，支持 Codex / ChatGPT Work、DeepSeek 与 OpenAI API。</p>
<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4" alt="Windows 10 / 11 x64">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/latest"><img src="https://img.shields.io/badge/version-3.2.1-10A37F" alt="Version 3.2.1"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>
<p align="center">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip"><strong>下载 Windows 版</strong></a> ·
  <a href="#快速开始">快速开始</a> ·
  <a href="docs/USER_GUIDE.zh-CN.md">使用指南</a>
</p>

<p align="center">
  <a href="README.md">简体中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.ru.md">Русский</a>
</p>

[![Token Monitor 控制中心与 ChatGPT 悬浮球](assets/readme-hero.png)](assets/readme-hero.png)

<p align="center"><sub>主界面与桌面悬浮球组合展示 · 模拟数据</sub></p>

## 用量就在手边

写代码、调用模型时，不必来回打开用量页面。Token Monitor 在一个概览中汇集三种服务，也能把你关心的数据留在桌面上。

- **轻巧悬浮球**：两行百分比分别显示 5 小时和每周剩余额度；可拖动，双击展开详情，支持五档大小。
- **独立浮窗**：ChatGPT、DeepSeek、OpenAI 各自开关，支持拖动与调整尺寸。隐藏浮窗后，已连接服务继续刷新。
- **本地历史趋势**：查看近 7 天／30 天变化，保留 90 天记录；缺失数据不补造。
- **按习惯显示**：浅色、深色、随系统主题，小／中／大字号，常驻左侧导航。
- **更新与回退**：应用内下载安装包，校验后确认重启；保留上一版，支持回退。

## 支持哪些数据

| 服务 | 可以查看 | 连接方式 |
| --- | --- | --- |
| **ChatGPT / Codex** | Codex 与 ChatGPT Work 共用的剩余额度、重置时间、可用重置次数 | 使用本机已登录的 Codex |
| **DeepSeek** | 可用余额、充值余额、赠送余额，按币种分别显示 | 手动 API Key 或本机 DeepSeek Harness 登录状态 |
| **OpenAI API** | 指定 API Key ID 的今日／本月 tokens、输入／输出／缓存输入、请求数 | 普通 API Key；历史用量另需 Admin API Key 与匹配的 Key ID |

ChatGPT 数据不涵盖普通聊天中所有模型的消息限额。OpenAI 统计范围为官方 `usage/completions` 接口覆盖的请求，缓存输入属于输入 tokens，不重复相加。

## 快速开始

**运行环境：Windows 10 / 11 x64、.NET Framework 4.8。** 运行时无需 Python、Node 或 .NET SDK。

1. [下载 Windows ZIP](https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip)，右键选择 **全部解压**。请下载运行包，而非 GitHub 自动生成的 `Source code`。
2. 运行 **Install.cmd**，选择程序与数据目录；安装助手会创建桌面和开始菜单快捷方式。也可直接运行解压后的 `TokenMonitor.exe`。
3. 从卡片的 **连接设置** 接入需要的服务，再按需打开浮窗或悬浮球。

自动刷新默认 **30 秒**，可在账户概览选择 15 / 30 / 60 / 120 秒或立即刷新。关闭主窗口会收起到托盘；双击托盘图标恢复，托盘菜单可彻底退出。

安装或连接遇到问题？查看 [安装与排错](docs/START_HERE.zh-CN.md) 和 [完整使用指南](docs/USER_GUIDE.zh-CN.md)。

### DeepSeek：API Key 或 Harness，任选其一

- **API Key**：在左栏「DeepSeek 密钥」中选择手动 API Key，填写并保存。
- **Harness 登录账户**：先在同一 Windows 用户下登录 DeepSeek Harness，再选择「DeepSeek 密钥 → DeepSeek 余额来源 → DeepSeek Harness 登录账户」并保存，无需保持 Harness 运行。

Harness 模式按币种合并充值与赠送钱包；凭据从默认本机位置按需读取，不复制到 Token Monitor。两种来源可以切换。当前不支持自定义 Harness 数据目录或非官方服务器，详见 [凭据处理说明](SECURITY.md)。

### OpenAI：验证密钥与读取用量分两步

先验证普通 API Key，再填写组织 **Admin API Key** 与对应的 **API Key ID**（`key_…`）。普通密钥验证成功不代表已获得历史用量权限；未配置完整时会显示下一步提示，不会误报零用量。具体步骤见 [OpenAI 连接说明](docs/USER_GUIDE.zh-CN.md)。

## 历史、更新与数据保存

### 查看 7 天／30 天趋势

左栏「历史趋势」显示 ChatGPT 剩余额度、DeepSeek **余额变化**与 OpenAI 每日 tokens，按北京时间（UTC+8）显示。

- ChatGPT 与 DeepSeek 每 15 分钟保留最新成功采样；按本机来源记录，切换登录账号会影响曲线。
- OpenAI 按 Key ID 摘要分别保存，每次查询修正已获取的本月每日值；过去月份仅保留已采集的数据，当天值尚未结算。
- **余额变化不等于消费。** 充值、赠送和退款都会影响余额，不能把余额下降直接当作消费金额。

### 在应用内更新

在「关于软件」检查更新、查看更新说明并下载运行包。SHA-256 与版本校验通过后，确认即可重启安装；原配置、密钥和历史保留，可从同页回退上一版。

程序启动时及成功检查后每 6 小时自动检查正式 Release。网络异常或 GitHub 限流时，用量监控继续运行，也可使用手动发布页入口。v3.1.0 及更早版本首次升级需要手动安装。

[查看发布记录](https://github.com/Ricki0613/TokenMonitor/releases) · [更新日志](CHANGELOG.md)

### 数据留在本机

正常运行和保存密钥不需要管理员权限。默认数据目录为 `%LOCALAPPDATA%\TokenMonitor`；在「数据目录」中选择可写空文件夹，可复制并校验配置、密钥与历史，原目录保留备份。

- DeepSeek 与 OpenAI 密钥使用 Windows DPAPI 按当前用户加密保存。
- Harness 登录凭据按需读取，只发送给对应的官方 DeepSeek 服务。
- 读取额度和用量不会发起模型对话；官方额度重置操作须单独确认。
- 更新检查不发送本机配置、密钥或账户用量。

完整数据范围与安全说明见 [SECURITY.md](SECURITY.md)。当前界面为中文，统计可能存在服务端延迟；暂无开机自启设置，发布程序尚未代码签名。

## 开发与贡献

欢迎 [提交问题或功能建议](https://github.com/Ricki0613/TokenMonitor/issues/new/choose)，或通过 Pull Request 改进工具。请勿在截图、日志或提交中包含真实凭据与账户数据。参与方式见 [贡献指南](CONTRIBUTING.md)。

<details>
<summary>从源码构建与验证</summary>

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

输出位于 `build/`。构建使用 .NET Framework C# 编译器；离线测试使用模拟数据，不需要真实账户或 API Key。运行 `scripts/package.ps1` 生成发布包，使用 `scripts/test-package.ps1 -Archive <ZIP路径>` 验证安装。

| 目录 | 内容 |
| --- | --- |
| `src/` | C# / WPF 源码 |
| `assets/` | 图标与产品展示图 |
| `scripts/` | 构建、测试、打包与展示图生成 |
| `installer/` | 安装助手 |
| `docs/` | 使用指南与发布说明 |

展示图由 `scripts/render-readme.ps1` 从原生 WPF 控件以双倍分辨率生成，使用模拟数据。维护者应发布带运行包的正式 Release 并标记为 Latest；仅推送 Git 标签不会触发应用更新提醒。

</details>

## 许可证

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

本项目为独立开源工具，与 OpenAI、DeepSeek 无官方隶属关系。
