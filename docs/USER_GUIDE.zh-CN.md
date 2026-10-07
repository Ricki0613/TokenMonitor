# Token Monitor

轻量 Windows 用量悬浮窗，安装于 `D:\Apps\TokenMonitor`。双击桌面 **Token Monitor** 图标启动。

## 使用

- 控制面板的 **ChatGPT** 和 **DeepSeek** 两个开关，分别显示或隐藏各自的悬浮窗。隐藏时暂停该服务的定时查询。
- 拖动窗口顶部即可移动。窗口位置、开关状态与刷新间隔会自动保存。
- 默认每 **30 秒**查询，可改为 15 / 30 / 60 / 120 秒，也可点“立即刷新”。服务限流时会等待后再试。
- GPT / DeepSeek 悬浮窗始终置顶。点击浮窗右上角关闭按钮，只隐藏该浮窗。
- 关闭控制面板会收起到系统托盘。双击托盘图标或再次双击桌面快捷方式可恢复面板。
- 点控制面板或托盘菜单中的“退出”可彻底退出，不会后台继续刷新。不自动设置开机启动。

## 数据口径

**ChatGPT 页**读取本机 Codex 已登录账号的 **Codex / ChatGPT Work 订阅额度**，显示官方返回的时间窗口、剩余百分比、重置时间以及额外 credits。当前 Plus 账号返回 5 小时及每周窗口。百分比由官方已用百分比换算；不会用本机消息数或 API 单价估算订阅额度。重置时间统一显示北京时间。到达重置时间后等待官方返回新数据，不自行补满进度条。

如果服务返回多个独立额度池，会分别显示。未返回的窗口、百分比与重置时间不会伪造。可用额度重置次数仅作展示，工具不会消耗重置权益。

**DeepSeek 页**通过提供的 API Key 查询其关联账户余额：可用余额、充值余额、赠送余额及 API 是否可调用。人民币与美元余额分别显示，不做汇率换算。余额接口不返回全账户累计 Token、逐次调用明细或历史消费，所以窗口中的“官方用量”入口可打开 DeepSeek 后台进一步查看。余额减少不能准确等同于 Token 消耗，工具不据此估算 Token。

数据默认每 30 秒轮询，可能存在服务端更新延迟。连接失败时保留并淡化上次成功的数据，同时标注“旧数据”和最后成功时间；首次连接失败显示原因。查询不会创建 GPT 或 DeepSeek 模型对话。

## 登录与密钥

- GPT 自动调用已安装的官方 Codex CLI，通过 `app-server` 的 `account/rateLimits/read` 获取额度。沿用本机 Codex 的正常登录机制，应用自身不读取或复制 OpenAI 登录令牌。请保持 Codex 已安装并登录所需账户；账号切换或登录失效后可重启本工具。
- DeepSeek API Key 存放于 `data\deepseek.key`，由 Windows DPAPI 按当前 Windows 用户加密，未写入程序源码或日志。该文件只能由同一 Windows 用户环境解密；迁移到另一账户或电脑时请重新填写密钥。
- 在“设置”内可更换 DeepSeek API Key；输入留空保留原密钥。程序只向固定的 `https://api.deepseek.com/user/balance` 地址发送该密钥，并拒绝自动跟随重定向。
- `data\settings.json` 只保存显示开关、位置及刷新间隔。程序不记录原始 API 响应或密钥日志。

## 文件与维护

- `TokenMonitor.exe`：主程序，无命令行窗口。
- `TokenMonitor.ico`：自制图标。
- `TokenMonitor.exe.config`：.NET Framework 4.8 运行配置。
- `source\`：完整 C# / WPF 源码及构建脚本，运行程序无需 Python、Node 或 .NET SDK。
- `data\`：本机偏好与加密密钥。

需 Windows 10/11 的 .NET Framework 4.8，以及已安装并登录的 Codex。构建使用系统自带的 C# 编译器。图标源生成脚本需要 Python/Pillow，但重新运行或使用主程序不需要它。

卸载前请通过托盘退出，再删除安装目录与桌面快捷方式。删除 `data` 会同时删除本工具保存的配置和加密密钥，不影响 Codex 登录。

## 官方资料

- [OpenAI：Codex App Server 账号与额度接口](https://learn.chatgpt.com/docs/app-server)
- [OpenAI：ChatGPT Work / Codex 订阅用量口径](https://learn.chatgpt.com/docs/pricing)
- [DeepSeek：查询账户余额接口](https://api-docs.deepseek.com/api/get-user-balance/)

版本：1.0.0 · 2026-10-07
