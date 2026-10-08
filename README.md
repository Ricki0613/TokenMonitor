<img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">

# Token Monitor

**Windows 桌面用量悬浮窗：随时查看 Codex / ChatGPT Work 额度与 DeepSeek API 余额。**

A lightweight Windows floating usage monitor for Codex / ChatGPT Work and DeepSeek. Built with C# and WPF.

[下载 Windows 版](https://github.com/Ricki0613/TokenMonitor/releases/download/v1.1.1/TokenMonitor-v1.1.1-windows-x64.zip) · [安装与排错](docs/START_HERE.zh-CN.md) · [使用指南](docs/USER_GUIDE.zh-CN.md) · [更新记录](CHANGELOG.md) · [English](README.en.md)

![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4)
![Version](https://img.shields.io/badge/version-1.1.1-10A37F)
![License](https://img.shields.io/badge/license-MIT-blue)

## 最近更新

| 版本 | 日期 | 主要变化 |
| --- | --- | --- |
| [v1.1.1](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.1.1) | 2026-10-08 | 修复自选安装目录下保存 Key 的权限问题；配置改存当前用户目录，自动迁移旧配置；新增安装助手、准确错误提示与安装排错说明 |
| [v1.1.0](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.1.0) | 2026-10-07 | 浮窗缩放与卡片重排、浅色/深色/随系统、官方 GPT 额度重置、紧凑高度 |
| [v1.0.0](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.0.0) | 2026-10-07 | 首版 GPT 额度与 DeepSeek 余额双浮窗、定时刷新、托盘和加密密钥 |

[完整更新记录](CHANGELOG.md) · [全部发布版本](https://github.com/Ricki0613/TokenMonitor/releases)

## 一眼看清两种用量

| ChatGPT 窗口 | DeepSeek 窗口 |
| --- | --- |
| Codex / ChatGPT Work 剩余额度百分比 | 可用余额 |
| 官方返回的 5 小时、每周等额度窗口 | 充值余额与赠送余额 |
| 北京时间重置时间与倒计时 | 人民币、美元分别显示 |
| 额外 credits 与其他官方额度池 | 余额是否支持继续调用 API |

两个窗口各自有显示开关，可以拖动、缩放、置顶、收起到托盘，并记住位置与尺寸。卡片随宽度重排，字号保持不变；默认高度紧贴内容，点击顶部 **↕** 可恢复贴合高度。设置支持 **浅色 / 深色 / 随系统**。默认每 **30 秒**刷新，可选 15 / 30 / 60 / 120 秒，也能随时手动刷新。网络异常时会标记旧数据与最后成功时间。

ChatGPT 浮窗新增 **重置额度**：显示官方剩余次数，确认后使用账户的一次重置权益，与 GPT 内使用重置次数相同。结果不确定时可安全重试，避免重复扣次数。

## 下载、安装与首次使用

1. **下载运行包**：[TokenMonitor-v1.1.1-windows-x64.zip](https://github.com/Ricki0613/TokenMonitor/releases/download/v1.1.1/TokenMonitor-v1.1.1-windows-x64.zip)，右键 → **全部解压**。
2. **安装**：双击解压后的 **Install.cmd**，按 Enter 使用默认位置，或输入自选完整路径。助手创建桌面、开始菜单快捷方式并启动程序。也可直接运行解压后的 `TokenMonitor.exe`。
3. **连接账户**：DeepSeek 在“设置”中填写自己的 API Key 并保存；GPT 先在本机安装并登录 Codex。只用其中一个服务时，关闭另一个开关即可。
4. **日常启动**：使用 **Token Monitor** 快捷方式。关闭控制面板会收起到托盘，双击托盘图标可恢复。

**正常运行和保存 Key 不需要管理员权限。** 安装位置可自选；配置会自动保存到 `%LOCALAPPDATA%\TokenMonitor`，安装包无需包含 `data` 文件夹。将运行文件复制到受保护目录时，Windows 可能要求一次文件操作授权。

**升级旧版**：先从托盘退出，将新文件覆盖到原目录并保留旧 `data`，正常启动后自动迁移可用配置。已有新版用户配置优先，旧文件保留。迁移完成后可再换安装位置。

遇到保存失败、加密失败或缺少运行环境时，按 [安装与排错说明](docs/START_HERE.zh-CN.md) 操作。设置页可查看数据目录及具体错误代码。

**系统要求**：Windows 10 / 11 x64、.NET Framework 4.8。监控 GPT 时需要安装并登录 Codex。运行主程序不需要 Python、Node 或 .NET SDK。

Release 中的 Windows ZIP 是可直接运行的程序；GitHub 自动生成的 `Source code` 压缩包用于开发。

## 数据范围与隐私

- GPT 显示 **Codex / ChatGPT Work 订阅额度**，不涵盖 ChatGPT 普通聊天的所有模型消息额度。
- DeepSeek 使用官方余额接口，**不是全账户 Token 历史统计**。点击窗口内“官方用量”可查看后台明细。
- 程序通过本机官方 Codex 查询 GPT 额度，应用自身不读取或复制 OpenAI 登录令牌。
- DeepSeek API Key 使用 Windows DPAPI 按当前用户加密保存，只用于请求官方余额接口。
- 缺失字段不会被当作零，到达重置时间后等待官方返回新额度；查询不会发起模型对话。

详细说明见 [使用指南](docs/USER_GUIDE.zh-CN.md) 和 [凭据处理说明](SECURITY.md)。

## 从源码构建

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

输出位于 `build\`。脚本使用 .NET Framework C# 编译器；30 项离线自测与 35 项界面检查使用模拟数据，包括禁止向程序目录写入时保存密钥、旧配置迁移和准确错误提示。无需真实账户或 API Key，不消耗重置次数。界面截图输出到 `build\ui-test-*\`。

发布包使用 `scripts/package.ps1` 生成；`scripts/test-package.ps1 -Archive <ZIP路径>` 执行 6 项安装检查，包括自选中文/空格路径、快捷方式、原地覆盖升级和损坏文件拦截。

| 目录 | 内容 |
| --- | --- |
| `src/` | C# / WPF 源码 |
| `assets/` | 程序图标 |
| `scripts/` | 构建、自测与图标生成脚本 |
| `installer/` | 普通用户安装助手与快捷方式创建 |
| `docs/` | 通用使用指南与发布说明 |
| `.github/` | Issue / Pull Request 模板 |

用户目录中的配置、密钥、旧版 `data/` 与构建产物不纳入版本控制。

## 参与贡献

欢迎 [提交问题或功能建议](https://github.com/Ricki0613/TokenMonitor/issues/new/choose)，也欢迎通过 Pull Request 改进工具。请写清楚复现步骤或使用场景，功能改动请运行构建、自测并检查界面。详见 [贡献指南](CONTRIBUTING.md)。

## 当前限制

- 当前界面为中文，重置时间使用北京时间（UTC+8）。
- 数据定时查询，可能存在服务端更新延迟；接口变化时可能需要适配。
- 暂无开机自启设置和自动更新；发布的程序尚未代码签名。

## 许可证

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

本项目为独立开源工具，与 OpenAI、DeepSeek 无官方隶属关系。
