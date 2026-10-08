# Token Monitor v1.1.1

修复用户在自选安装目录中保存 Key/设置失败的问题，并补齐安装、升级与排错流程。

## 本次更新

- **正常运行无需管理员权限**：配置和加密密钥改存 `%LOCALAPPDATA%\TokenMonitor`，由程序自动创建。运行文件可放在自选位置，ZIP 无需包含 `data` 文件夹。
- **兼容旧配置**：在原目录升级后，自动复制旧 `data` 中的有效配置与当前用户可解密的密钥；旧文件保留，已有新版配置优先。
- **准确报错**：区分目录权限、Windows 加密、文件读写和其他异常，显示操作及错误代码；设置中可打开数据目录。
- **安装助手**：ZIP 内双击 `Install.cmd`，确认默认路径或输入自选完整路径，创建桌面与开始菜单快捷方式并启动程序。
- **完善文档**：新增安装“先读我”、故障处理表；仓库首页展示 v1.1.1、v1.1.0、v1.0.0 更新摘要。

## 首次安装

下载 **TokenMonitor-v1.1.1-windows-x64.zip** → 右键“全部解压” → 双击 **Install.cmd** → 按提示确认安装目录。也可直接运行解压后的 `TokenMonitor.exe`。

DeepSeek：设置中输入自己的 API Key。GPT：先安装并登录 Codex。只使用其中一个服务时可关闭另一个开关。

系统要求：Windows 10 / 11 x64、.NET Framework 4.8。运行无需 Python、Node 或 Git。复制程序文件到受保护目录时 Windows 仍可能要求授权，日常运行和保存配置不需要管理员权限。

## 旧版升级

先从托盘退出，完整解压到原程序目录并覆盖，保留旧 `data`，用平常的 Windows 用户启动一次即可迁移。迁移完成后可自由移动程序。若以前使用其他管理员账户保存 Key，按提示重新输入。

## 验证

30 项离线自测及 35 项界面检查通过，包含禁止向程序目录写入时仍能保存 Key/设置、自动创建数据目录、旧版迁移、不覆盖新配置、错误分类与临时文件清理。测试使用模拟 Key，不消耗 GPT 重置次数。

6 项发布包安装检查通过：完整性与编码、自选中文/空格路径、桌面快捷方式目标、原地重新安装保留数据，以及文件损坏时停止安装。

ZIP 包附带安装助手、运行文件、安装与使用说明、许可证和 SHA-256 清单，不含个人配置或密钥。程序尚未代码签名。

[完整安装与排错说明](https://github.com/Ricki0613/TokenMonitor/blob/main/docs/START_HERE.zh-CN.md) · [使用指南](https://github.com/Ricki0613/TokenMonitor/blob/main/docs/USER_GUIDE.zh-CN.md) · [历次更新](https://github.com/Ricki0613/TokenMonitor/blob/main/CHANGELOG.md)
