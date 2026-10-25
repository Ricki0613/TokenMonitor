using System;
namespace TokenMonitor {
    public static class OpenAiConnectionHelp {
        public static string Next(OpenAiConnection connection) {
            if(connection==null)return "尚未验证密钥 · 先填写普通 API Key。";
            if(string.IsNullOrEmpty(connection.AdminKey))return "密钥已保存 · 历史用量未连接。下一步：添加管理员提供的 Admin API Key。";
            if(string.IsNullOrEmpty(connection.ApiKeyId))return "密钥已保存 · 历史用量未连接。下一步：填写对应的 key_ 标识。";
            return "密钥已保存 · 点击保存密钥设置，验证历史用量连接。";
        }
    }
}
