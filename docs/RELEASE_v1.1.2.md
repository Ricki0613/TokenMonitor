# Token Monitor v1.1.2

安装位置与数据目录都可以自行选择。

## 新增

- **安装时选择数据目录**：双击 ZIP 中的 `Install.cmd`，可分别填写程序位置与数据位置；按 Enter 则保留当前/默认选择。
- **设置中随时更改**：打开“设置 → 选择数据目录”，新建或选择可写空文件夹。
- **自动迁移**：配置、加密密钥和未确认的重置请求复制并校验成功后，才启用新路径。原目录保留备份。
- **保存路径选择**：重启后继续使用所选目录，同一 Windows 用户的不同程序副本沿用同一选择。
- 目标非空、没有写权限或无法保存路径选择时，原目录继续使用，界面显示处理提示。

## 下载与升级

下载 **TokenMonitor-v1.1.2-windows-x64.zip**，右键“全部解压”，运行 **Install.cmd**。也可直接运行解压后的 `TokenMonitor.exe`，在设置中选择数据目录。

从 v1.1.1 升级：先从托盘退出并覆盖运行文件，已有配置继续有效。早期版本请在原目录覆盖升级、保留旧 `data` 并运行一次，以完成迁移。

默认数据目录仍是 `%LOCALAPPDATA%\TokenMonitor`。自选位置需要当前 Windows 用户有写入权限；正常运行无需管理员权限。系统要求：Windows 10 / 11 x64、.NET Framework 4.8。

## 验证

36 项离线自测、35 项界面检查、6 项安装包检查通过。新增覆盖迁移后保存、保留密钥和重置请求、非空目标保护、权限拒绝与路径保存失败恢复。测试不消耗账户重置次数。

安装包附运行文件、安装助手、安装及使用说明、许可证与 SHA-256 校验清单，不包含个人配置或密钥。程序尚未代码签名。

[安装与排错](https://github.com/Ricki0613/TokenMonitor/blob/main/docs/START_HERE.zh-CN.md) · [使用指南](https://github.com/Ricki0613/TokenMonitor/blob/main/docs/USER_GUIDE.zh-CN.md) · [历次更新](https://github.com/Ricki0613/TokenMonitor/blob/main/CHANGELOG.md)
