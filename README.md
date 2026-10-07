<img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">

# Token Monitor

**Windows 桌面用量悬浮窗：随时查看 Codex / ChatGPT Work 额度与 DeepSeek API 余额。**

A lightweight Windows floating usage monitor for Codex / ChatGPT Work and DeepSeek. Built with C# and WPF.

[下载 Windows 版](https://github.com/Ricki0613/TokenMonitor/releases/latest/download/TokenMonitor-v1.0.0-windows-x64.zip) · [使用指南](docs/USER_GUIDE.zh-CN.md) · [反馈问题](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) · [English](README.en.md)

![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-0078D4)
![Version](https://img.shields.io/badge/version-1.0.0-10A37F)
![License](https://img.shields.io/badge/license-MIT-blue)

## 一眼看清两种用量

| ChatGPT 窗口 | DeepSeek 窗口 |
| --- | --- |
| Codex / ChatGPT Work 剩余额度百分比 | 可用余额 |
| 官方返回的 5 小时、每周等额度窗口 | 充值余额与赠送余额 |
| 北京时间重置时间与倒计时 | 人民币、美元分别显示 |
| 额外 credits 与其他官方额度池 | 余额是否支持继续调用 API |

两个窗口各自有显示开关，可以拖动、置顶、收起到托盘，并记住位置。默认每 **30 秒**刷新，可选 15 / 30 / 60 / 120 秒，也能随时手动刷新。网络异常时会标记旧数据与最后成功时间。

## 三步开始

1. **下载并解压**：[Windows x64 程序包](https://github.com/Ricki0613/TokenMonitor/releases/latest/download/TokenMonitor-v1.0.0-windows-x64.zip)。放在一个固定、可写的目录，然后双击 `TokenMonitor.exe`。无需编译。
2. **连接账户**：GPT 沿用本机已经安装并登录的 Codex；DeepSeek 在工具的“设置”中填写自己的 API Key。
3. **打开需要的悬浮窗**：使用控制面板上的两个开关。关闭控制面板会收起到托盘，双击托盘图标可恢复。

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
```

输出位于 `build\`。脚本使用 .NET Framework C# 编译器；12 项离线自测无需真实账户或 API Key，不请求 GPT / DeepSeek 服务。

| 目录 | 内容 |
| --- | --- |
| `src/` | C# / WPF 源码 |
| `assets/` | 程序图标 |
| `scripts/` | 构建、自测与图标生成脚本 |
| `docs/` | 通用使用指南与发布说明 |
| `.github/` | Issue / Pull Request 模板 |

运行时的 `data/`、密钥、偏好与构建产物不纳入版本控制。

## 参与贡献

欢迎 [提交问题或功能建议](https://github.com/Ricki0613/TokenMonitor/issues/new/choose)，也欢迎通过 Pull Request 改进工具。请写清楚复现步骤或使用场景，功能改动请运行构建、自测并检查界面。详见 [贡献指南](CONTRIBUTING.md)。

## 当前限制

- 当前界面为中文，重置时间使用北京时间（UTC+8）。
- 数据定时查询，可能存在服务端更新延迟；接口变化时可能需要适配。
- 暂无开机自启设置和自动更新；发布的程序尚未代码签名。

## 许可证

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

本项目为独立开源工具，与 OpenAI、DeepSeek 无官方隶属关系。
