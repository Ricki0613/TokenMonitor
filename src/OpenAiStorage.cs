using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TokenMonitor {
    public static class OpenAiSecretStore {
        static readonly byte[] Entropy=Encoding.UTF8.GetBytes("TokenMonitor.OpenAI.v2");
        public static void Validate(OpenAiConnection connection) {
            if(connection==null)throw new ArgumentException("请输入有效的 OpenAI API Key。");
            try {
                OpenAiProvider.RequireSecret(connection.ApiKey,false);
                if(!string.IsNullOrEmpty(connection.AdminKey))OpenAiProvider.RequireSecret(connection.AdminKey,true);
                if(!string.IsNullOrEmpty(connection.ApiKeyId))OpenAiProvider.RequireKeyId(connection.ApiKeyId);
            } catch(ProviderException e) {throw new ArgumentException(e.Message);}
        }
        public static void Save(OpenAiConnection connection) {
            Validate(connection);
            byte[] plain=Encoding.UTF8.GetBytes(Json.Write(connection));
            try {Directory.CreateDirectory(Paths.Data);Atomic.Write(Paths.OpenAiKey,ProtectedData.Protect(plain,Entropy,DataProtectionScope.CurrentUser));}
            finally {Array.Clear(plain,0,plain.Length);}
        }
        public static OpenAiConnection Read() {
            if(!File.Exists(Paths.OpenAiKey))throw new ProviderException("请在设置中使用 OpenAI API Key 登录。",300);
            return Decode(File.ReadAllBytes(Paths.OpenAiKey));
        }
        static OpenAiConnection Decode(byte[] encrypted) {
            byte[] plain=ProtectedData.Unprotect(encrypted,Entropy,DataProtectionScope.CurrentUser);
            try {
                var obj=Json.Read(Encoding.UTF8.GetString(plain));
                var connection=new OpenAiConnection {ApiKey=Json.Str(obj,"ApiKey"),AdminKey=Json.Str(obj,"AdminKey"),ApiKeyId=Json.Str(obj,"ApiKeyId")};
                Validate(connection);return connection;
            } finally {Array.Clear(plain,0,plain.Length);}
        }
        internal static void ValidateEncrypted(byte[] encrypted) {Decode(encrypted);}
    }
}
