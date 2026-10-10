# Token Monitor 使用指南

## 安装与启动

1. 从 [发布页](https://github.com/Ricki0613/TokenMonitor/releases) 下载对应版本的 Windows ZIP，选择“全部解压”。v2.0.0 的运行包为 `TokenMonitor-v2.0.0-windows-x64.zip`。
2. 双击 `Install.cmd`，按 Enter 接受默认位置，或分别输入程序位置和数据目录。助手创建桌面和开始菜单快捷方式并启动程序。也可直接运行解压后的 `TokenMonitor.exe`。
3. 确保系统为 Windows 10 / 11 x64，并有 .NET Framework 4.8。详细安装和排错步骤见 [先读我](START_HERE.zh-CN.md)。

正常运行、填写 Key 和保存设置不需要管理员权限。运行文件的位置可自选；复制到受保护目录时，Windows 可能要求文件操作授权。用户数据默认自动保存在 `%LOCALAPPDATA%\TokenMonitor`，也可自行选择可写数据目录。

无需安装 Python、Node 或 .NET SDK。需要桌面入口时，右键 `TokenMonitor.exe` 创建快捷方式并放到桌面即可。工具不会自动设置开机启动。

## 配置三个服务

### GPT

先在本机安装并登录 Codex。Token Monitor 自动查找 Codex 桌面应用附带的 CLI，或 `PATH` 中的 `codex.exe`，通过官方 app-server 查询登录账号的订阅额度。

如果只使用 DeepSeek，可关闭 GPT 开关。账号切换或登录过期后，请先在 Codex 完成登录，再重启 Token Monitor。

### DeepSeek

打开控制面板中的“设置”，填写你自己的 DeepSeek API Key 并保存。已有密钥时输入框留空可保留原值。

密钥使用 Windows DPAPI 按当前用户加密，存放在当前数据目录的 `deepseek.key`。设置页显示实际位置，并提供“打开数据目录”和“选择数据目录”。换电脑或 Windows 用户时需重新填写，不要把该文件分享给其他人。

### OpenAI API

OpenAI API 浮窗与 ChatGPT / Codex 订阅浮窗分别配置。API Key 登录不会替代或修改 Codex 登录。

1. 在“设置”的 OpenAI 区域填写自己的普通 **API Key**，点击 **连接 OpenAI** 验证。程序仅调用 `GET https://api.openai.com/v1/models`，不发起对话或消耗模型 Token。若使用受限 Key，需要允许读取模型列表；读取权限不足也会导致验证失败。
2. 若只填写普通 Key，登录成功后浮窗显示**待配置历史用量**，需要进一步填写 **Admin API Key** 和 **API Key ID**，不能将这个状态视为零用量。成功登录会开启 OpenAI 浮窗；也可通过控制面板开关控制显示。
3. 由组织所有者在 [OpenAI 组织 Admin keys](https://platform.openai.com/settings/organization/admin-keys) 创建 Admin API Key，并将它填入管理密钥栏。普通项目 API Key 无法代替 Admin API Key 读取组织历史用量。
4. 找到需要监控的普通 Key 的准确 **API Key ID**。它是 `key_…` 标识，**不是** `sk-…` 秘密值，也不是项目 ID、组织 ID、Key 名称或 Secret 的末尾几位。可用 Admin API Key 通过官方只读接口 `GET /v1/organization/projects` 查询项目，再用 `GET /v1/organization/projects/{project_id}/api_keys?owner_project_access=any` 查询对应项目的 Key 列表。核对名称、创建时间和脱敏 Secret，复制对应记录的 `id`。列表的 `has_more` 为 true 时需用 `last_id` 作为下一页的 `after` 查询；工具不会自动推导匹配关系。
5. 补齐管理密钥和 Key ID 后再次点击 **连接 OpenAI** 并刷新，查看该 Key 的今日、本月用量。确保 Admin Key 所属组织和 Key 所属项目相符。更换普通 API Key 时，原 Key ID 关联会被清除，需重新指定并确认。

普通 API Key、Admin API Key 与目标 Key ID 一起以当前 Windows 用户 DPAPI 加密保存在当前数据目录的 `openai.key`。已保存的普通 Key 和 Admin Key 输入框留空可保留原值；不要把 `openai.key` 或 Admin Key 分享给其他人。Admin Key 具有组织管理权限，工具仅使用它读取用量。

### 自选数据目录

打开“设置 → 选择数据目录”，新建或选择一个可写空文件夹。当前配置、`deepseek.key`、`openai.key` 和未确认的重置请求会自动复制、校验，成功后才启用新路径，原目录保留备份。路径选择即时生效并按 Windows 用户记住，关闭设置或重启后仍有效。

不要手工剪切数据文件来换路径。目标非空、无写权限或路径选择无法保存时，程序继续使用原目录，并显示处理提示。自选目录位于其他磁盘时，请保证该磁盘在启动程序时可用。

## 日常操作

- **显示开关**：ChatGPT、DeepSeek、OpenAI API 分别控制各自的悬浮窗；隐藏后暂停对应服务的定时刷新。托盘菜单也可分别显示或隐藏。
- **移动**：拖动窗口顶部。窗口位置、开关状态与刷新间隔自动保存。
- **缩放**：拖动浮窗任意边缘或角落。变宽时额度/余额卡片并排，变窄时改为纵向与按钮换行，字号保持不变。最小宽度 300，避免内容过窄。
- **贴合内容**：默认高度随内容变化，保留固定边距。手动调整高度后，较长内容可滚动；点击顶部 **↕** 恢复自动贴合高度。浮窗尺寸和贴合模式会保存。
- **外观**：在“设置 → 外观”选择浅色、深色或随系统，点击保存后所有窗口更新。“随系统”自动响应 Windows 应用主题变化。
- **刷新**：默认 30 秒，可选择 15 / 30 / 60 / 120 秒；“立即刷新”可手动查询。
- **收起**：关闭控制面板会收起到托盘；双击托盘图标或再次启动程序可恢复。
- **隐藏浮窗**：点击浮窗右上角关闭按钮，只隐藏对应窗口。
- **退出**：从控制面板或托盘菜单点击“退出”，停止后台刷新。

## 数据说明

### GPT：Codex / ChatGPT Work 订阅额度

显示官方返回的时间窗口、剩余比例、北京时间重置时间、额外 credits 等。常见窗口为 5 小时和每周，实际以当前账号的接口返回为准。

这些数据不代表 ChatGPT 普通聊天所有模型的消息限制，也不按 OpenAI API 单价估算。未返回的窗口、比例和重置时间不会被伪造；到达重置时间后等待官方刷新数据，不自动把进度条补满。

### 使用 GPT 额度重置次数

ChatGPT 浮窗的 **重置额度 (N)** 显示当前账号的官方剩余次数。点击后再次确认，工具调用官方 `account/rateLimitResetCredit/consume`，使用一次账户重置权益，与 GPT 内使用重置次数相同。实际可重置的窗口由官方决定；成功后重新查询额度与剩余次数。

无次数、次数未知、显示旧数据或正在查询时，按钮禁用。官方可能返回“没有可重置窗口”或“没有可用次数”，工具会显示该结果。旧 Codex 不支持接口时会提示升级。

如果请求超时或断线，按钮变为 **确认上次重置**，复用保存在本机的请求标识，避免重复使用次数。重启后仍可确认上次结果。工具不会定时或自动使用重置次数。

### DeepSeek：API 账户余额

显示可用余额、充值余额、赠送余额，以及是否可调用 API。人民币和美元分别显示，不换算或相加。

余额接口不提供全账户累计 Token 或历史消费明细；请点击“官方用量”查看后台。工具不会通过余额差额估算 Token 数。

### OpenAI：指定 API Key 的 Token 用量

使用组织 Admin API Key 查询官方 `GET /v1/organization/usage/completions`，按提供的 `api_key_ids` 筛选并处理分页，分别汇总北京时间（UTC+8）今日零点至当前、本月第一天零点至当前的用量。统计不是最近 24 小时或最近 30 天。

- **输入 Token**：官方返回的 `input_tokens`。
- **输出 Token**：官方返回的 `output_tokens`。
- **缓存输入 Token**：官方返回的 `input_cached_tokens`，已包含在输入 Token 中，不能重复加入输入与输出总量。
- **请求数**：官方返回的 `num_model_requests`。

范围是官方 completions 用量接口覆盖的模型请求，字段可能包含该接口汇总的文本、音频或图像 Token。它不等于全部 API 端点的总用量；图像生成、嵌入、音频等独立用量接口不在本窗口的查询范围内。以 OpenAI 官方接口当前定义为准。

此窗口不显示 API 余额、可用额度、剩余 Token 或 ChatGPT 订阅用量，也不估算金额。官方统计可能延迟；只有查询成功且返回的范围中没有记录时才显示零。缺少凭据、失败或必需字段未知时显示配置提示、错误或未知状态。

## 异常状态

| 提示 | 处理方法 |
| --- | --- |
| 未找到 Codex / 登录失败 | 安装 Codex，完成登录后重启工具 |
| DeepSeek 密钥无效或无权访问 | 在“设置”中更新自己的 API Key |
| OpenAI API Key 登录失败 | 核对普通 Key 是否有效、组织/项目权限及 `GET /v1/models` 读取权限 |
| OpenAI 待配置历史用量 | 在设置中补齐组织 Admin API Key 与对应的 API Key ID |
| OpenAI 管理密钥无效或权限不足 | 由组织所有者确认 Admin Key，并核对目标 Key 是否属于该组织 |
| OpenAI 用量为零但官方有记录 | 核对准确的 Key ID、北京时间统计范围，并等待官方统计更新 |
| 无法解密密钥 | 在当前 Windows 用户下重新保存 API Key |
| 用户数据目录访问被拒绝 | 在设置中查看目录，检查该目录权限及安全软件的拦截记录 |
| Windows 用户加密失败 | 使用平常的 Windows 账户正常登录后重试；旧密钥无法解密时重新输入 |
| 无法读写用户数据 | 检查磁盘空间和文件占用；记录操作名称与错误代码用于反馈 |
| 网络失败或超时 | 检查网络，等待自动重试或手动刷新 |
| 查询频率受限 | 等待工具按服务端要求重试 |

连接失败时保留并淡化旧数据，标注最后成功时间。首次获取失败显示错误，不把未知值当作零。默认定时轮询，可能存在服务端更新延迟。

## 文件与卸载

`TokenMonitor.exe`、`TokenMonitor.exe.config`、`TokenMonitor.ico` 是运行文件。下载包还附带安装助手、安装与使用说明、许可证及校验清单。

卸载前先从托盘退出，再删除程序目录、桌面及开始菜单中的 Token Monitor 快捷方式。如需清除账户配置，另行删除设置页显示的当前数据目录；旧版 `data` 和迁移备份若保留也可一并清理。这不会影响 Codex 的登录。

## 参考资料

- [OpenAI：Codex App Server](https://learn.chatgpt.com/docs/app-server)
- [OpenAI：订阅用量](https://learn.chatgpt.com/docs/pricing)
- [OpenAI：组织管理 API 与 Admin API Key](https://developers.openai.com/api/reference/administration/overview)
- [OpenAI：Completions 用量接口](https://developers.openai.com/api/reference/resources/admin/subresources/organization/subresources/usage/methods/completions)
- [OpenAI：列出项目 API Key](https://developers.openai.com/api/reference/resources/admin/subresources/organization/subresources/projects/subresources/api_keys/methods/list)
- [DeepSeek：余额接口](https://api-docs.deepseek.com/api/get-user-balance/)

## 从 v1.0.0 / v1.1.0 升级

先从托盘退出程序，再将 v2.0.0 解压到原目录，覆盖运行文件并保留 `data/`。也可在安装助手中选择原目录。第一次启动会把旧设置及当前 Windows 用户可解密的密钥复制到当前数据目录；旧文件保留，已有新版配置不会被旧数据覆盖。

迁移后位置、开关、刷新间隔、主题与尺寸继续有效。若要更换安装位置，请先在原目录启动一次完成迁移。曾用其他管理员账户保存的 Key 可能无法解密，按提示重新输入即可；正常使用无需管理员运行。

从 v1.1.1 / v1.1.2 升级时继续读取已保存的用户配置。安装和设置中都可进一步选择数据目录。GPT、DeepSeek 的位置、显示状态等设置保留，新增 OpenAI 窗口默认关闭；完成 OpenAI API Key 登录后启用。

版本：2.0.0
