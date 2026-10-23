# v2.2.0 — DeepSeek Harness 账户余额

发布日期：2026-10-23

新增“设置 → DeepSeek 余额来源 → DeepSeek Harness 登录账户”。先在同一 Windows 用户下登录官方 Harness，保存设置后即可查询余额，无需另填 API Key，也无需保持 Harness 窗口打开。

- 读取 `%USERPROFILE%\.dsh\.credentials.yaml` 中的账户授权，每次查询重新读取，跟随 Harness 重新登录。
- 使用官方 Platform 账户摘要接口，按币种展示充值钱包、赠送钱包及其合计；不推断 API 可调用状态。
- 只向固定的 `https://platform.deepseek.com` 发送授权，禁用重定向，不复制或保存 Harness 凭据，不修改 Harness 文件。
- 保留原有手动 API Key 模式；旧配置默认继续使用原模式。切换来源会清除旧余额，避免跨来源混淆。
- 登录缺失或过期时提示回 Harness 登录；查询失败保留并标记同来源旧数据，不把失败显示成零。

兼容性：已依据 Harness Desktop 0.2.1-rc.2 的本机格式验证。本功能使用 Harness 当前账户格式与 Platform 接口，后续 Harness 变更可能需要适配；自定义数据根目录和非官方服务器暂不支持。

升级：退出 Token Monitor，覆盖程序文件并保留数据目录，或运行安装包内 Install.cmd 选择原目录。
