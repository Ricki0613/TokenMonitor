using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace TokenMonitor {
    public static class LocalStorage {
        public static string Notice;
        public static void Initialize() {
            try {
                Notice=Migrate(Path.Combine(Paths.Root,"data"));
                if(Paths.TestDataDirectory==null&&!string.IsNullOrEmpty(StorageLocation.LoadWarning))Notice=StorageLocation.LoadWarning+(Notice==null?"":"\n"+Notice);
            }
            catch(Exception e) {Notice=Explain(e,"准备用户数据目录");}
        }
        // Copy only missing, validated files. Existing per-user data always wins.
        public static string Migrate(string legacy) {
            Directory.CreateDirectory(Paths.Data);
            if(string.Equals(Path.GetFullPath(legacy).TrimEnd('\\'),Path.GetFullPath(Paths.Data).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))return null;
            var notes=new List<string>();
            foreach(string name in new[]{"settings.json","deepseek.key"}) {
                string source=Path.Combine(legacy,name),destination=Path.Combine(Paths.Data,name);
                if(File.Exists(destination)||!File.Exists(source))continue;
                try {
                    byte[] bytes=File.ReadAllBytes(source);
                    if(name=="deepseek.key")SecretStore.ValidateEncrypted(bytes);
                    else {
                        try {
                            var value=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(source));
                            if(value==null)throw new InvalidDataException();
                        }catch(ArgumentException) {throw new InvalidDataException();}
                    }
                    Atomic.Write(destination,bytes);
                } catch(CryptographicException) {notes.Add("旧密钥无法由当前 Windows 用户解密，请在设置中重新输入 API Key。");}
                catch(Exception e) {notes.Add(Explain(e,name=="deepseek.key"?"迁移旧密钥":"迁移旧设置"));}
            }
            return notes.Count==0?null:string.Join("\n",notes);
        }
        public static string Explain(Exception error,string operation) {
            string advice;
            if(error is UnauthorizedAccessException||error is SecurityException)
                advice="用户数据目录访问被拒绝。请检查该目录的权限或安全软件拦截。";
            else if(error is CryptographicException)
                advice="Windows 用户加密失败。请使用自己的 Windows 账户正常登录后重试；旧密钥无法解密时请重新输入。";
            else if(error is InvalidDataException)
                advice="配置文件格式无效，请保留原文件并重新设置。";
            else if(error is IOException)
                advice="无法读写用户数据。请检查磁盘空间、文件占用及目录是否可用。";
            else advice="发生未预期错误，请将下面的错误代码提供给维护者。";
            return operation+"失败："+advice+"\n错误代码："+error.GetType().Name+" / 0x"+error.HResult.ToString("X8");
        }
    }
}
