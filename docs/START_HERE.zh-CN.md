# Token Monitor v2.0.0：先读我

## 第一次安装（推荐）

1. 从 GitHub Releases 下载对应版本的 Windows ZIP。v2.0.0 的运行包为 **TokenMonitor-v2.0.0-windows-x64.zip**。`Source code` 是源码，不是运行包。
2. 右键 ZIP → **全部解压**。打开解压后的文件夹。
3. 双击 **Install.cmd**。按 Enter 使用默认安装位置，也可输入自己选择的完整路径，如 `C:\Tools\TokenMonitor`。下一步可保留当前/默认数据目录，或指定可写空文件夹，如 `C:\Tools\TokenMonitorData`。
4. 安装助手复制运行文件、创建桌面和开始菜单快捷方式，并打开程序。之后用 **Token Monitor** 快捷方式启动。
5. 只用 DeepSeek：在“设置”填入自己的 API Key，保存后看 DeepSeek 浮窗。使用 GPT：先安装并登录 Codex，再打开 ChatGPT 开关。只打开需要的服务。
6. 使用 OpenAI API：在“设置”使用普通 API Key 登录；要看对应 Key 的历史 Token 用量，还需组织 **Admin API Key** 和准确的 **API Key ID**。填写方法见下文。

默认安装到 `%LOCALAPPDATA%\Programs\TokenMonitor`，不需要管理员权限。不需要 Python、Node、Git 或编译环境。安装助手使用 Windows 自带 PowerShell；如果脚本被单位策略禁用，可使用下面的直接运行方式。

## 自选位置直接运行

将整个 ZIP 解压到自己选择的位置，直接双击 **TokenMonitor.exe** 即可。`TokenMonitor.exe.config`、`TokenMonitor.ico` 应与主程序放在同一个文件夹。移动程序时一起移动这三个文件。

程序可位于其他磁盘或受保护目录；复制文件到受保护目录时 Windows 可能要求授权，但正常运行和保存 Key 不需要管理员权限。不要只从压缩包里双击主程序。

## 配置和密钥保存在哪里

默认自动创建 **`%LOCALAPPDATA%\TokenMonitor`**。安装时可以自选，也可打开 **设置 → 选择数据目录**，新建或选择一个可写空文件夹。程序会复制并校验当前配置与加密密钥（包括 `deepseek.key`、`openai.key`），成功后切换；重启后沿用所选路径。原目录文件保留作为备份。

安装包不需要携带 `data` 文件夹。“设置 → 打开数据目录”始终打开当前实际使用的位置。路径选择按当前 Windows 用户保存；同一用户启动其他副本也会使用这个路径。

换安装位置不会丢失当前 Windows 用户的配置。换电脑或 Windows 用户时，请重新输入自己的 Key。

## OpenAI API 配置要点

- 普通 **API Key** 用于登录验证。工具只读取 `GET /v1/models`，不发送推理请求；受限 Key 必须允许读取该接口。
- 历史用量必须额外配置 **Admin API Key** 与 **API Key ID**。只有普通 Key 时会提示待配置，未知数据不会显示成零。
- 组织所有者可在 [OpenAI Admin keys](https://platform.openai.com/settings/organization/admin-keys) 创建管理密钥。不要把 Admin Key 填到普通 Key 栏，也不要将普通 Key 当作 Admin Key。
- **API Key ID** 是对应普通 Key 的 `key_…` 标识，与 `sk-…` Secret 不同。通过官方只读 `GET /v1/organization/projects/{project_id}/api_keys?owner_project_access=any` 查询对应项目的密钥列表，核对名称、创建时间和脱敏值，复制对应记录的 `id`。更换普通 Key 后需重新确认该 ID，详细步骤见使用指南。
- 浮窗按北京时间统计今日、本月输入、输出、缓存输入 Token 与请求数，缓存输入已包含在输入中。范围为官方 completions 用量接口覆盖的模型请求，不显示余额、剩余 Token 或 ChatGPT 订阅额度。

详细步骤及官方接口链接见 [使用指南](USER_GUIDE.zh-CN.md)。

## 从旧版升级

1. 从旧程序的托盘菜单选择 **退出**。
2. 将新 ZIP 完整解压到原程序文件夹并覆盖运行文件，**保留旧 `data` 文件夹**。也可以在安装助手中选择原安装目录。
3. 以平常使用的 Windows 用户启动一次。旧 `data` 中的设置和可解密密钥会自动复制到用户数据目录；旧文件保留，已有新版配置优先。
4. 若要换安装位置，完成上一步迁移后再移动程序或运行安装助手。

从 v1.1.1 / v1.1.2 升级时会继续使用已保存的用户配置。需要换数据目录时使用安装助手或软件设置中的选择功能。新增 OpenAI 窗口默认关闭，成功登录后开启；已有 GPT、DeepSeek 配置保留。

以前设置过“始终以管理员身份运行”的，请在主程序和快捷方式的属性中取消该选项后正常启动。如果以前用另一个管理员账户保存 Key，新版会提示重新输入。

## 无法启动或保存时

| 现象 | 处理 |
| --- | --- |
| 找不到主程序、图标或配置文件 | 确认下载的是 Windows ZIP，并已完整解压 |
| 提示缺少 .NET Framework | 安装微软 [.NET Framework 4.8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)，然后重新启动工具 |
| Windows 提示未知发布者 | 当前版本未签名。核对来自项目官方 Release 及校验文件，再根据自己的设备策略决定是否运行 |
| 安装助手无法写入目标目录 | 重新运行并选择有写入权限的安装位置；也可直接运行完整解压的程序 |
| 用户数据访问被拒绝 | 打开设置查看数据目录，检查该目录权限及安全软件的拦截记录 |
| Windows 用户加密失败 | 使用自己的 Windows 账户正常登录；若旧密钥无法解密，重新输入 Key |
| 无法读写用户数据 | 检查磁盘空间、文件占用，记录界面上的操作名称和错误代码 |
| 目标数据文件夹已有内容 | 新建或选择一个空文件夹，避免覆盖现有文件 |
| 切换数据目录失败 | 原目录继续使用；检查目标写权限。若目标已留下副本，请保留备份并另选空目录重试 |
| Key 保存成功但余额查询报错 | 按浮窗提示检查 Key 是否有效、网络与账户余额权限 |
| OpenAI 登录成功但显示待配置 | 补齐组织 Admin API Key 与准确的 API Key ID |
| OpenAI Key 登录被拒绝 | 检查 Key 有效性及读取模型列表的权限 |
| OpenAI 历史用量无权访问 | 确认填入的是 Admin API Key，且目标 Key 属于该组织 |

仍无法解决时，在 GitHub 提交系统版本、程序版本、失败操作及错误代码。不要附带 API/Admin Key、`deepseek.key` 或 `openai.key`。

系统要求：**Windows 10 / 11 x64、.NET Framework 4.8**。完整操作见同目录 `USER_GUIDE.zh-CN.md`。

[项目首页](https://github.com/Ricki0613/TokenMonitor) · [反馈问题](https://github.com/Ricki0613/TokenMonitor/issues)
