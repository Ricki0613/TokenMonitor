# Token Monitor 使用指南

## 安装与启动

1. 从 [最新发布页](https://github.com/Ricki0613/TokenMonitor/releases/latest) 下载 `TokenMonitor-v1.0.0-windows-x64.zip`。
2. 解压到固定、可写的目录。请先解压，再运行其中的 `TokenMonitor.exe`。
3. 确保系统为 Windows 10 / 11 x64，并有 .NET Framework 4.8。

无需安装 Python、Node 或 .NET SDK。需要桌面入口时，右键 `TokenMonitor.exe` 创建快捷方式并放到桌面即可。工具不会自动设置开机启动。

## 配置两个服务

### GPT

先在本机安装并登录 Codex。Token Monitor 自动查找 Codex 桌面应用附带的 CLI，或 `PATH` 中的 `codex.exe`，通过官方 app-server 查询登录账号的订阅额度。

如果只使用 DeepSeek，可关闭 GPT 开关。账号切换或登录过期后，请先在 Codex 完成登录，再重启 Token Monitor。

### DeepSeek

打开控制面板中的“设置”，填写你自己的 DeepSeek API Key 并保存。已有密钥时输入框留空可保留原值。

密钥使用 Windows DPAPI 按当前用户加密，存放在程序目录的 `data/deepseek.key`。换电脑或 Windows 用户时需重新填写，不要把该文件分享给其他人。

## 日常操作

- **显示开关**：ChatGPT、DeepSeek 分别控制各自的悬浮窗；隐藏后暂停对应服务的定时刷新。
- **移动**：拖动窗口顶部。窗口位置、开关状态与刷新间隔自动保存。
- **刷新**：默认 30 秒，可选择 15 / 30 / 60 / 120 秒；“立即刷新”可手动查询。
- **收起**：关闭控制面板会收起到托盘；双击托盘图标或再次启动程序可恢复。
- **隐藏浮窗**：点击浮窗右上角关闭按钮，只隐藏对应窗口。
- **退出**：从控制面板或托盘菜单点击“退出”，停止后台刷新。

## 数据说明

### GPT：Codex / ChatGPT Work 订阅额度

显示官方返回的时间窗口、剩余比例、北京时间重置时间、额外 credits 等。常见窗口为 5 小时和每周，实际以当前账号的接口返回为准。

这些数据不代表 ChatGPT 普通聊天所有模型的消息限制，也不按 OpenAI API 单价估算。未返回的窗口、比例和重置时间不会被伪造；到达重置时间后等待官方刷新数据，不自动把进度条补满。额度重置权益的次数仅供展示，工具不会使用该权益。

### DeepSeek：API 账户余额

显示可用余额、充值余额、赠送余额，以及是否可调用 API。人民币和美元分别显示，不换算或相加。

余额接口不提供全账户累计 Token 或历史消费明细；请点击“官方用量”查看后台。工具不会通过余额差额估算 Token 数。

## 异常状态

| 提示 | 处理方法 |
| --- | --- |
| 未找到 Codex / 登录失败 | 安装 Codex，完成登录后重启工具 |
| DeepSeek 密钥无效或无权访问 | 在“设置”中更新自己的 API Key |
| 无法解密密钥 | 在当前 Windows 用户下重新保存 API Key |
| 网络失败或超时 | 检查网络，等待自动重试或手动刷新 |
| 查询频率受限 | 等待工具按服务端要求重试 |

连接失败时保留并淡化旧数据，标注最后成功时间。首次获取失败显示错误，不把未知值当作零。默认定时轮询，可能存在服务端更新延迟。

## 文件与卸载

`TokenMonitor.exe`、`TokenMonitor.exe.config`、`TokenMonitor.ico` 是运行文件。`data/` 是运行后生成的本机密钥与偏好。下载包还附带通用说明及许可证。

卸载前先从托盘退出，再删除程序目录与快捷方式。删除 `data/` 会清除本工具保存的配置，不影响 Codex 的登录。

## 参考资料

- [OpenAI：Codex App Server](https://learn.chatgpt.com/docs/app-server)
- [OpenAI：订阅用量](https://learn.chatgpt.com/docs/pricing)
- [DeepSeek：余额接口](https://api-docs.deepseek.com/api/get-user-balance/)

版本：1.0.0
