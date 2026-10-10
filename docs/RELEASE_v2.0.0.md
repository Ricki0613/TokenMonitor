# Token Monitor v2.0.0

2026-10-10

新增 OpenAI API Key 登录与第三个独立用量浮窗，查看指定 Key 的今日、本月 Token 和请求数。

## 新增

- **OpenAI API 登录**：在“设置”填入普通 API Key，使用官方 `GET /v1/models` 验证。受限 Key 需允许读取模型列表；验证不发送模型推理请求、不消耗模型 Token。
- **按 Key 统计**：补齐组织 Admin API Key 与目标 API Key ID 后，通过官方 `usage/completions` 接口获取输入、输出、缓存输入 Token 和请求数。今日、本月以北京时间（UTC+8）划分。
- **独立浮窗**：OpenAI 具有单独显示开关、托盘入口、窗口位置与尺寸保存；支持主题、拖动、缩放、手动/定时刷新、旧数据提示与失败退避。
- **加密保存**：OpenAI API/Admin Key 和目标 Key ID 统一以当前 Windows 用户 DPAPI 加密保存于 `openai.key`。旧数据迁移与自选数据目录切换同时处理该文件。
- **保留旧配置**：原 GPT、DeepSeek 设置继续有效；旧配置中的新增 OpenAI 开关默认关闭，登录后开启。

## 配置与统计范围

普通 OpenAI API Key 不能单独读取组织历史用量。需要组织所有者在 [Admin keys](https://platform.openai.com/settings/organization/admin-keys) 创建管理密钥，并提供对应普通 Key 的准确 `key_…` ID；此 ID 与 `sk-…` Secret 不同。可用官方只读项目 Key 列表接口 `GET /v1/organization/projects/{project_id}/api_keys?owner_project_access=any` 查询，核对名称、创建时间和脱敏值，复制对应记录的 `id`。工具不自动推导二者关联，更换普通 Key 时需重新确认 ID。

仅普通 Key 登录成功时显示待配置提示，不以零代替未知数据。缓存输入是输入 Token 的子集，不重复相加。统计范围为官方 completions 用量接口覆盖的模型请求，不是全部 API 端点的总用量，也不显示 API 余额、剩余 Token 或 ChatGPT 订阅额度。官方用量可能存在更新延迟。

## 安装与升级

运行包为 **TokenMonitor-v2.0.0-windows-x64.zip**。完整解压后运行 **Install.cmd**，或直接运行 `TokenMonitor.exe`。系统要求：Windows 10 / 11 x64、.NET Framework 4.8；正常运行及保存 Key 无需管理员权限。

升级前先从旧版托盘退出，再覆盖程序文件。v1.1.1 / v1.1.2 沿用已有用户数据目录；更早版本请在原目录覆盖、保留旧 `data` 并运行一次完成自动迁移。自选数据路径保持有效，原数据保留。

## 验证与分发

构建、58 项离线自测、54 项模拟界面检查和 6 项安装包检查均通过。覆盖 OpenAI 用量解析、分页、异常响应、凭据加密、目录迁移、独立开关、三种主题及设置中的密码输入框。安装包的分发检查覆盖包内容、脚本编码、自选目录、快捷方式、覆盖升级和损坏文件拦截。测试不需要真实账户或 API Key，不发送推理请求、不使用额度重置次数；实际服务数据仍受组织权限、Key ID 对应关系及官方更新延迟影响。

运行包附安装助手、安装及使用说明、许可证与 SHA-256 校验清单，不包含个人配置或密钥。程序尚未代码签名。

[安装与排错](START_HERE.zh-CN.md) · [使用指南](USER_GUIDE.zh-CN.md) · [更新记录](../CHANGELOG.md)
