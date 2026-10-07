# 参与 Token Monitor

欢迎修复问题、完善文档、改进界面或提出功能建议。

## 反馈问题

在 [Issues](https://github.com/Ricki0613/TokenMonitor/issues/new/choose) 选择相应模板，说明版本、复现步骤、实际行为和预期行为。涉及新功能时，优先描述使用场景。

截图或日志请先去掉 API Key、账户标识和个人用量。不要上传 `data/` 或 Codex 登录文件。

## 提交修改

1. Fork 仓库并创建描述清楚的分支，例如 `fix/window-position`。
2. 完成一项范围明确的修改，保持使用说明与实际行为一致。
3. 运行 `scripts/build.ps1` 和 `scripts/test.ps1`；涉及界面时实际打开检查。
4. 发起 Pull Request，说明解决的问题、修改后的行为和验证方式。关联已有 Issue（如有）。

离线自测无需真实账户。不要把真实 API Key 放进测试或提交记录。

## 代码位置

- `src/Providers.cs`：官方服务连接。
- `src/Models.cs`：数据解析、配置和密钥存储。
- `src/Views.cs`、`src/Theme.xaml`：WPF 界面。
- `src/Program.cs`：窗口生命周期、刷新与托盘。
- `src/SelfTests.cs`：离线自测。

项目采用 [MIT License](LICENSE)，提交贡献时请确保你有权提供相应内容。
