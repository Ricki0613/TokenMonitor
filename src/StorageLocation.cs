using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace TokenMonitor {
    public static class StorageLocation {
        static string current;
        public static string LoadWarning;
        public static string Current {get {
            if(current==null) {
                try {
                    using(var key=Registry.CurrentUser.OpenSubKey(@"Software\TokenMonitor")) {
                        string value=key==null?null:key.GetValue("DataDirectory") as string;
                        current=string.IsNullOrWhiteSpace(value)?Paths.DefaultData:Normalize(value);
                    }
                } catch(Exception e) {current=Paths.DefaultData;LoadWarning=LocalStorage.Explain(e,"读取数据目录选择")+"\n已暂用默认目录，请在设置中重新选择。";}
            }
            return current;
        } }
        static string Normalize(string path) {
            if(string.IsNullOrWhiteSpace(path)||!Path.IsPathRooted(path)||
                !(path.StartsWith(@"\\")||(path.Length>=3&&path[1]==':'&&(path[2]=='\\'||path[2]=='/'))))
                throw new ArgumentException("请选择数据目录的完整路径。");
            string full=Path.GetFullPath(path);
            if(string.Equals(full,Path.GetPathRoot(full),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("请选择专用数据文件夹，不能直接使用磁盘根目录。");
            return full.TrimEnd('\\','/');
        }
        static void Remember(string path) {
            using(var key=Registry.CurrentUser.CreateSubKey(@"Software\TokenMonitor"))key.SetValue("DataDirectory",path,RegistryValueKind.String);
        }
        // Commit the location only after both files are copied and verified. The source remains a backup.
        public static void Relocate(string destination,Preferences preferences,Action<string> remember=null) {
            destination=Normalize(destination);
            string source=Normalize(Paths.Data);
            if(string.Equals(source,destination,StringComparison.OrdinalIgnoreCase))return;
            if(Directory.Exists(destination)&&Directory.EnumerateFileSystemEntries(destination).Any())
                throw new ArgumentException("目标文件夹已有内容，请新建或选择一个空文件夹。现有数据不会被覆盖。");
            if(!Directory.Exists(source))throw new IOException("当前数据目录不可用");
            byte[] encrypted=null;
            try {encrypted=File.ReadAllBytes(Path.Combine(source,"deepseek.key"));}
            catch(FileNotFoundException) {}
            byte[] settings=Encoding.UTF8.GetBytes(Json.Write(preferences));
            Directory.CreateDirectory(destination);
            string settingsPath=Path.Combine(destination,"settings.json"),keyPath=Path.Combine(destination,"deepseek.key");
            Atomic.Write(settingsPath,settings);
            if(encrypted!=null)Atomic.Write(keyPath,encrypted);
            if(!File.ReadAllBytes(settingsPath).SequenceEqual(settings)||
                (encrypted!=null&&!File.ReadAllBytes(keyPath).SequenceEqual(encrypted)))throw new IOException("数据校验失败");
            (remember??Remember)(destination);
            if(Paths.TestDataDirectory!=null)Paths.TestDataDirectory=destination;else current=destination;
            LoadWarning=null;LocalStorage.Notice=null;
        }
    }
}
