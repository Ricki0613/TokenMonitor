# Token Monitor v1.0.0

Windows 桌面用量悬浮窗，支持 **Codex / ChatGPT Work 订阅额度**与 **DeepSeek API 余额**。

## 下载与使用

下载下方 **TokenMonitor-v1.0.0-windows-x64.zip**，解压后双击 `TokenMonitor.exe`。

1. GPT：先安装并登录本机 Codex。
2. DeepSeek：在工具“设置”中输入自己的 API Key。
3. 在控制面板打开需要的悬浮窗。

系统要求：Windows 10 / 11 x64、.NET Framework 4.8。运行无需 Python、Node 或 .NET SDK。界面为中文，重置时间显示北京时间。

## 功能

- 两个独立悬浮窗及显示开关，可拖动、置顶、记住位置。
- GPT 官方额度窗口、剩余比例、重置时间及 credits。
- DeepSeek 可用余额、充值余额、赠送余额与多币种显示。
- 自动/手动刷新、托盘、单实例恢复及旧数据标记。
- DeepSeek API Key 通过 Windows DPAPI 加密保存。

GPT 显示 Codex / ChatGPT Work 额度；DeepSeek 显示余额，Token 明细需在官方后台查看。[完整使用指南](https://github.com/Ricki0613/TokenMonitor/blob/main/docs/USER_GUIDE.zh-CN.md)

## 下载包与源码

Windows ZIP 包含运行文件、使用说明、MIT 许可证和校验清单，不包含个人密钥、用量数据、开发教程或源码目录。源码及构建方法见 [项目首页](https://github.com/Ricki0613/TokenMonitor)。

本次仅整理开源说明与下载包，程序功能和 `v1.0.0` 可执行文件保持一致。原始 Git 标签保留版本历史，当前项目许可见 [MIT License](https://github.com/Ricki0613/TokenMonitor/blob/main/LICENSE)。

`TokenMonitor.exe` SHA-256：

```text
371a3ddb3c1351e11f1f4c734e9eeb7893ffa71a5656f80b78f9f8a2d159d240
```

12 项离线自测通过。程序尚未代码签名，不提供开机自启设置或自动更新。
