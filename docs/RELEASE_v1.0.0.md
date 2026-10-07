# v1.0.0 · 第一版

Windows 桌面 GPT / DeepSeek 用量悬浮窗的首个稳定备份版本。

## 下载后怎么用

下载 **TokenMonitor-v1.0.0-windows-x64.zip**，解压到固定目录，双击 `TokenMonitor.exe`。

- 需要 Windows 10 / 11 x64 和 .NET Framework 4.8。
- GPT 页沿用本机 Codex 的登录账号；请先安装并登录 Codex。
- DeepSeek 页首次使用时在“设置”中填写自己的 API Key。
- 此程序包不包含任何用户的密钥、登录令牌或个人运行配置。

## 包含的功能

- GPT 与 DeepSeek 两个独立悬浮窗及显示开关。
- Codex / ChatGPT Work 5 小时、每周剩余额度与重置时间。
- DeepSeek 可用余额、充值余额、赠送余额与多币种显示。
- 定时与手动刷新、置顶、拖动、位置记忆、托盘及单实例恢复。
- Windows DPAPI 密钥加密，连接异常时标记旧数据。

DeepSeek 此页展示余额，Token 明细请通过“官方用量”打开后台查看。GPT 此页展示 Codex / ChatGPT Work 额度，数据以官方接口返回为准。

## 验证和完整性

12 项离线自测通过；第一版开发时已经验证真实服务连接、显示开关、布局、快捷方式与单实例行为。此次重新构建使用的应用源码与已安装第一版一致。

ZIP 中保留的是实际安装过的原始第一版可执行文件，其 SHA-256 为：

```text
371a3ddb3c1351e11f1f4c734e9eeb7893ffa71a5656f80b78f9f8a2d159d240
```

程序包同时包含源码、构建脚本、使用说明和 GitHub 入门指南。程序未进行代码签名，没有自动更新功能。
