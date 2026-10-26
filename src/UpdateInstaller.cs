using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace TokenMonitor {
    public sealed class UpdatePlan {
        public string Target,Source,Backup;
        public int ProcessId;
        public string[] Hashes;
    }
    public static class UpdateInstaller {
        internal static readonly string[] Files={"TokenMonitor.exe","TokenMonitor.exe.config","TokenMonitor.ico"};
        public static string Previous {get{return Path.Combine(Paths.Root,"previous-version");}}
        public static bool CanRollback {get{return Files.All(n=>File.Exists(Path.Combine(Previous,n)))&&File.Exists(Path.Combine(Previous,"hashes.json"));}}
        public static string Hash(string file) {using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        public static void Verify(string file,string hash) {if(string.IsNullOrEmpty(hash)||!System.Text.RegularExpressions.Regex.IsMatch(hash,"\\A[0-9a-fA-F]{64}\\z")||!string.Equals(Hash(file),hash,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("下载文件 SHA-256 校验失败，未更改当前安装。");}
        public static void Extract(string zip,string destination,Version version) {
            Directory.CreateDirectory(destination);
            using(var archive=ZipFile.OpenRead(zip))foreach(string name in Files) {
                var entries=archive.Entries.Where(e=>e.FullName==name).ToArray();
                if(entries.Length!=1||entries[0].Length<=0||entries[0].Length>52428800)throw new InvalidDataException("安装包文件缺失或异常。");
                entries[0].ExtractToFile(Path.Combine(destination,name));
            }
            var actual=FileVersionInfo.GetVersionInfo(Path.Combine(destination,Files[0])).FileVersion;
            if(actual!=version.ToString(4))throw new InvalidDataException("安装包版本与发布版本不一致。");
        }
        public static async Task<string> Download(AvailableRelease release) {
            if(string.IsNullOrEmpty(release.AssetHash))throw new InvalidDataException("此版本没有可验证的 SHA-256，请使用发布页手动安装。");
            string dir=Path.Combine(Paths.Root,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            string zip=Path.Combine(dir,"package.zip");
            using(var client=new HttpClient {Timeout=TimeSpan.FromMinutes(5)}) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("TokenMonitor/"+UpdateClient.Current.ToString(3));
                using(var response=await client.GetAsync(release.AssetUrl,HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false)) {
                    response.EnsureSuccessStatusCode();
                    using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var output=File.Create(zip)) {
                        var buffer=new byte[81920];int read;long total=0;
                        while((read=await input.ReadAsync(buffer,0,buffer.Length).ConfigureAwait(false))>0){total+=read;if(total>52428800)throw new InvalidDataException("安装包超出大小限制。");await output.WriteAsync(buffer,0,read).ConfigureAwait(false);}
                    }
                }
            }
            Verify(zip,release.AssetHash);string stage=Path.Combine(dir,"stage");Extract(zip,stage,release.Version);return stage;
        }
        public static void Launch(string source) {
            string dir=Path.Combine(Paths.Root,"updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            string helper=Path.Combine(dir,"updater.exe");File.Copy(Path.Combine(Paths.Root,Files[0]),helper);
            File.Copy(Path.Combine(Paths.Root,Files[1]),helper+".config");
            var plan=new UpdatePlan {Target=Path.GetFullPath(Paths.Root),Source=Path.GetFullPath(source),Backup=Path.Combine(dir,"backup"),ProcessId=Process.GetCurrentProcess().Id,Hashes=Files.Select(n=>Hash(Path.Combine(source,n))).ToArray()};
            if(string.Equals(source.TrimEnd('\\'),Previous.TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)) {
                var hashes=new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(Path.Combine(source,"hashes.json")));
                if(hashes==null||hashes.Length!=Files.Length)throw new InvalidDataException("上一版校验信息无效。");
                for(int i=0;i<Files.Length;i++)Verify(Path.Combine(source,Files[i]),hashes[i]);
            }
            string manifest=Path.Combine(dir,"plan.json");Atomic.Write(manifest,Encoding.UTF8.GetBytes(Json.Write(plan)));
            Process.Start(new ProcessStartInfo(helper,"--apply-update \""+manifest+"\"") {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
        }
        internal static void Apply(UpdatePlan plan) {
            if(plan.Hashes==null||plan.Hashes.Length!=Files.Length)throw new InvalidDataException();
            for(int i=0;i<Files.Length;i++)Verify(Path.Combine(plan.Source,Files[i]),plan.Hashes[i]);
            Directory.CreateDirectory(plan.Backup);
            // Finish the entire backup before touching any installed file.
            foreach(string name in Files)File.Copy(Path.Combine(plan.Target,name),Path.Combine(plan.Backup,name));
            try {
                for(int i=0;i<Files.Length;i++) {File.Copy(Path.Combine(plan.Source,Files[i]),Path.Combine(plan.Target,Files[i]),true);Verify(Path.Combine(plan.Target,Files[i]),plan.Hashes[i]);}
            }catch {foreach(string name in Files)File.Copy(Path.Combine(plan.Backup,name),Path.Combine(plan.Target,name),true);throw;}
            string previous=Path.Combine(plan.Target,"previous-version");Directory.CreateDirectory(previous);
            foreach(string name in Files)File.Copy(Path.Combine(plan.Backup,name),Path.Combine(previous,name),true);
            Atomic.Write(Path.Combine(previous,"hashes.json"),Encoding.UTF8.GetBytes(Json.Write(Files.Select(n=>Hash(Path.Combine(previous,n))).ToArray())));
        }
        public static int Run(string manifest) {
            UpdatePlan plan=null;
            try {
                plan=new JavaScriptSerializer().Deserialize<UpdatePlan>(File.ReadAllText(manifest));
                try {var old=Process.GetProcessById(plan.ProcessId);if(!old.WaitForExit(60000))throw new IOException("应用尚未退出，更新已取消。");}catch(ArgumentException) { }
                Apply(plan);
                Process.Start(new ProcessStartInfo(Path.Combine(plan.Target,Files[0])) {UseShellExecute=true});return 0;
            }catch(Exception e) {
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(manifest),"update-error.txt"),e.Message);
                MessageBox.Show("更新未完成，旧版备份保留在 updates 文件夹。\n"+e.Message,"Token Monitor",MessageBoxButton.OK,MessageBoxImage.Warning);
                if(plan!=null)try{Process.Start(new ProcessStartInfo(Path.Combine(plan.Target,Files[0])) {UseShellExecute=true});}catch { }
                return 1;
            }
        }
    }
    public sealed partial class MonitorApp {
        bool installBusy;
        public async Task InstallUpdate() {
            if(installBusy)return;if(AvailableUpdate==null){OpenUpdate();return;}
            var release=AvailableUpdate;installBusy=true;Main.DownloadUpdate.IsEnabled=false;
            try {
                Main.UpdateStatus.Text="正在下载安装包并校验 SHA-256…";
                string stage=await UpdateInstaller.Download(release);
                Main.UpdateStatus.Text="校验通过，准备安装 "+release.Tag+"。";
                if(MessageBox.Show(Main,"安装包已校验通过。现在退出并更新到 "+release.Tag+"？\n配置与历史记录保留，上一版可在关于软件中回退。","重启更新",MessageBoxButton.OKCancel,MessageBoxImage.Question)!=MessageBoxResult.OK)return;
                UpdateInstaller.Launch(stage);Quit();
            }catch(Exception e){Main.UpdateStatus.Text=e is InvalidDataException?e.Message:"下载或准备更新失败，当前安装未更改；请重试或打开发布页。";}
            finally {installBusy=false;Main.DownloadUpdate.IsEnabled=true;}
        }
        public void RollbackUpdate() {
            if(installBusy||!UpdateInstaller.CanRollback)return;
            if(MessageBox.Show(Main,"退出并恢复上一版程序？当前配置与历史记录保留。","回退上一版",MessageBoxButton.OKCancel,MessageBoxImage.Question)!=MessageBoxResult.OK)return;
            try {UpdateInstaller.Launch(UpdateInstaller.Previous);Quit();}catch{Main.UpdateStatus.Text="无法准备回退，请检查上一版文件和安装目录权限。";}
        }
    }
}
