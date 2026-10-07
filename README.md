<img src="assets/TokenMonitor.png" width="72" alt="Token Monitor 图标">

# Token Monitor

**把 GPT 额度和 DeepSeek 余额放在桌面上，随时看一眼。**

一个用 C# / WPF 编写的 Windows 小工具。两个独立悬浮窗、两个显示开关、自动刷新和系统托盘，专注查看已有账户的官方用量数据。

当前版本：**v1.0.0** · Windows 10 / 11 x64 · .NET Framework 4.8

> 这是第一版的源码及发布包备份。仓库与发布包不含任何 API Key、Codex 登录令牌、本机偏好或实际账户用量快照。

## 下载和开始使用

1. 打开 [v1.0.0 发布页](https://github.com/Ricki0613/TokenMonitor/releases/tag/v1.0.0)。
2. 在 Assets 中下载 **TokenMonitor-v1.0.0-windows-x64.zip**。`Source code` 是源码压缩包，不是可直接运行的程序包。
3. 将程序包解压到固定目录，例如 `D:\Apps\TokenMonitor`，双击 `TokenMonitor.exe`。
4. GPT 需要本机已安装并登录 Codex；DeepSeek 首次使用时在“设置”里填写自己的 API Key。
5. 若需要桌面快捷方式，在资源管理器中为 `TokenMonitor.exe` 创建快捷方式并放到桌面即可。

从 GitHub 下载的副本需要重新配置 DeepSeek 密钥。已有电脑上的加密配置保留在本机，未上传到本仓库。

完整操作说明见 [使用指南](docs/USER_GUIDE.zh-CN.md)。

## 两个窗口分别显示什么

| 页面 | 显示内容 | 数据来源 |
| --- | --- | --- |
| ChatGPT | Codex / ChatGPT Work 的 5 小时及每周剩余额度、重置时间、额外 credits、官方返回的其他额度池 | 本机 Codex 的 `account/rateLimits/read` |
| DeepSeek | 可用余额、充值余额、赠送余额，以及是否可调用 API | DeepSeek 官方 `GET /user/balance` |

**GPT 页显示 Codex / ChatGPT Work 订阅额度。**它不代表 ChatGPT 普通聊天的所有模型消息额度，也不使用 OpenAI API 账单推算 Plus 剩余量。

**DeepSeek 页显示账户余额。**余额接口不返回全账户历史 Token 统计，窗口中的“官方用量”按钮可打开后台查看详细记录。程序不会把余额变化换算成不准确的 Token 数。

## 第一版功能

- 两个显示开关，各自控制一个悬浮窗；隐藏后暂停该服务的定时查询。
- 悬浮窗置顶、可拖动，并记住位置与显示状态。
- 默认 30 秒刷新，可选 15 / 30 / 60 / 120 秒，也可手动刷新。
- GPT 剩余额度进度条、北京时间重置时间与倒计时。
- DeepSeek 多币种余额分别显示，不合并或换算人民币与美元。
- 连接异常时淡化旧数据，并显示最后成功时间；缺失数据不会被当作零。
- 控制面板可收起到系统托盘；重复启动会唤回现有实例。
- DeepSeek 密钥使用 Windows DPAPI 按当前用户加密保存。
- 查询过程不发起模型对话。

## 本地构建与测试

运行程序不需要 Python、Node 或 .NET SDK。构建脚本使用 Windows 上的 .NET Framework C# 编译器。

在仓库根目录打开 PowerShell：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
```

构建输出位于 `build\`。自测只检查解析、边界情况、本机配置与 DPAPI，不需要真实账号或密钥，不访问 GPT / DeepSeek 服务。图标文件已包含在仓库中，无需安装绘图库。

第一版开发时还验证了两项真实账户连接、独立显示开关、置顶、收起恢复及界面布局。涉及账户的数据和截图不随仓库保存。可执行文件的校验值见 [第一版备份记录](docs/V1_BACKUP.md)。

## 目录

```text
src/          第一版 C# / WPF 源码
assets/       程序图标
scripts/      构建、自测脚本
docs/         使用说明、GitHub 入门、备份记录
.github/      问题反馈及 Pull Request 模板
CHANGELOG.md  版本变化记录
SECURITY.md   凭据和本机数据处理说明
```

`build/`、`data/`、密钥、测试输出与二进制发布包均被 `.gitignore` 排除。可下载的程序放在 **Releases** 中，源码历史留在仓库中。

## 后续怎么维护

发现问题或想到功能，可以先建一个 [Issue](https://github.com/Ricki0613/TokenMonitor/issues/new/choose)。修改时创建独立分支，完成测试后发起 Pull Request，再合并到 `main`。稳定版本使用新的标签与 Release 留档。

第一次使用 GitHub？请看 [以 Token Monitor 为例的中文入门指南](docs/GITHUB_GUIDE.zh-CN.md)。

## 已知边界

- 这是定时轮询工具，显示的新鲜程度取决于刷新间隔与官方服务的更新时间。
- GPT 依赖已安装的 Codex 及有效登录；官方接口发生变化时可能需要适配。
- 第一版没有开机自启设置；需要时手动通过快捷方式启动。
- 程序包未进行代码签名，也没有自动更新功能。
- 本仓库是个人项目备份，未选择开源许可证。

本工具与 OpenAI、DeepSeek 无官方隶属关系。

## 参考资料

- [OpenAI：Codex App Server](https://learn.chatgpt.com/docs/app-server)
- [OpenAI：Codex / ChatGPT Work 订阅用量](https://learn.chatgpt.com/docs/pricing)
- [DeepSeek：查询余额](https://api-docs.deepseek.com/api/get-user-balance/)
