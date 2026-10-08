using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitor {
    public sealed class ProviderException : Exception {
        public int Cooldown;
        public ProviderException(string message,int cooldown=0):base(message) { Cooldown=cooldown; }
    }
    public sealed class DeepProvider : IDisposable {
        readonly HttpClient http;
        public DeepProvider() {
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            http=new HttpClient(new HttpClientHandler { AllowAutoRedirect=false });
            http.Timeout=TimeSpan.FromSeconds(20);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenMonitor/1.1.2");
        }
        public async Task<DeepSnapshot> Fetch() {
            string key=SecretStore.Read();
            using(var request=new HttpRequestMessage(HttpMethod.Get,"https://api.deepseek.com/user/balance")) {
                request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",key);
                key=null;
                using(var response=await http.SendAsync(request).ConfigureAwait(false)) {
                    int code=(int)response.StatusCode;
                    if(code==401||code==403) throw new ProviderException("密钥无效或无权访问，请在设置中更新",300);
                    if(code==429) {
                        int cooldown=60;
                        if(response.Headers.RetryAfter!=null) {
                            if(response.Headers.RetryAfter.Delta.HasValue) cooldown=(int)Math.Min(3600,Math.Max(60,response.Headers.RetryAfter.Delta.Value.TotalSeconds));
                            else if(response.Headers.RetryAfter.Date.HasValue) cooldown=(int)Math.Min(3600,Math.Max(60,(response.Headers.RetryAfter.Date.Value-DateTimeOffset.UtcNow).TotalSeconds));
                        }
                        throw new ProviderException("查询频率受限，稍后自动重试",cooldown);
                    }
                    if(!response.IsSuccessStatusCode) throw new ProviderException("官方服务暂不可用（HTTP "+code+"）",60);
                    return DeepSnapshot.Parse(Json.Read(await response.Content.ReadAsStringAsync().ConfigureAwait(false)));
                }
            }
        }
        public void Dispose() { http.Dispose(); }
    }
    public sealed class CodexProvider : IDisposable {
        Process process;
        int nextId;
        readonly object gate=new object();
        readonly SemaphoreSlim startup=new SemaphoreSlim(1,1);
        readonly Dictionary<int,TaskCompletionSource<Dictionary<string,object>>> pending=new Dictionary<int,TaskCompletionSource<Dictionary<string,object>>>();
        bool disposed;
        public static string Locate() {
            var local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var bin=Path.Combine(local,"OpenAI","Codex","bin");
            if(Directory.Exists(bin)) {
                foreach(var folder in new DirectoryInfo(bin).GetDirectories().OrderByDescending(x=>x.LastWriteTimeUtc)) {
                    string candidate=Path.Combine(folder.FullName,"codex.exe");
                    if(File.Exists(candidate)) return candidate;
                }
            }
            foreach(var path in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) {
                try { string candidate=Path.Combine(path.Trim('"'),"codex.exe");if(File.Exists(candidate)) return candidate; } catch {}
            }
            throw new ProviderException("未找到 Codex，请先安装并登录 Codex 桌面应用");
        }
        async Task Ensure() {
            await startup.WaitAsync().ConfigureAwait(false);
            try {
                if(disposed) throw new ObjectDisposedException("CodexProvider");
                if(process!=null&&!process.HasExited) return;
                var info=new ProcessStartInfo(Locate(),"app-server --stdio") {
                    UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,
                    StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=Paths.Root
                };
                var p=new Process { StartInfo=info,EnableRaisingEvents=true };
                p.ErrorDataReceived+=(s,e)=>{}; // Drain diagnostics; never log credentials or raw server responses.
                if(!p.Start()) throw new ProviderException("无法启动 Codex 用量连接");
                process=p;p.BeginErrorReadLine();
                Task.Run(()=>ReadLoop(p));
                await Request("initialize",new { clientInfo=new { name="token_monitor",title="Token Monitor",version="1.1.2" } }).ConfigureAwait(false);
                Send(new { method="initialized",@params=new {} });
            } catch { StopProcess();throw; } finally { startup.Release(); }
        }
        void ReadLoop(Process owner) {
            try {
                string line;
                while((line=owner.StandardOutput.ReadLine())!=null) {
                    Dictionary<string,object> obj;
                    try { obj=Json.Read(line); } catch { continue; }
                    var id=Json.Number(obj,"id");
                    if(!id.HasValue) continue;
                    TaskCompletionSource<Dictionary<string,object>> source=null;
                    lock(gate) { if(ReferenceEquals(owner,process) && pending.TryGetValue((int)id.Value,out source)) pending.Remove((int)id.Value); }
                    if(source!=null) source.TrySetResult(obj);
                }
            } catch {} finally {
                lock(gate) {
                    if(ReferenceEquals(owner,process)) {
                        foreach(var t in pending.Values) t.TrySetException(new ProviderException("Codex 连接已断开，稍后自动重连"));
                        pending.Clear();
                    }
                }
            }
        }
        void Send(object obj) { lock(gate) { process.StandardInput.WriteLine(Json.Write(obj));process.StandardInput.Flush(); } }
        async Task<Dictionary<string,object>> Request(string method,object parameters) {
            int id=Interlocked.Increment(ref nextId);
            var source=new TaskCompletionSource<Dictionary<string,object>>();
            lock(gate) pending[id]=source;
            try {
                Send(new { id=id,method=method,@params=parameters });
                if(await Task.WhenAny(source.Task,Task.Delay(25000)).ConfigureAwait(false)!=source.Task) throw new TimeoutException();
                var obj=await source.Task.ConfigureAwait(false);
                if(Json.Get(obj,"error")!=null) {
                    string msg=Json.Str(Json.Child(obj,"error"),"message")??"";
                    if(method=="account/rateLimitResetCredit/consume") {
                        if(Json.Number(Json.Child(obj,"error"),"code")==-32601)
                            throw new ProviderException("当前 Codex 版本不支持额度重置，请更新 Codex 后重试");
                        throw new ProviderException("官方未确认重置结果，请重试确认上次结果");
                    }
                    if(msg.IndexOf("auth",StringComparison.OrdinalIgnoreCase)>=0 || msg.IndexOf("login",StringComparison.OrdinalIgnoreCase)>=0 || msg.IndexOf("401",StringComparison.Ordinal)>=0)
                        throw new ProviderException("登录状态已过期，请先在 Codex 桌面应用中登录",120);
                    throw new ProviderException("官方用量查询失败，请确认 Codex 已登录后重试",60);
                }
                var result=Json.Child(obj,"result");
                if(result==null) throw new ProviderException("官方未返回用量数据");
                return result;
            } finally { lock(gate) pending.Remove(id); }
        }
        public async Task<GptSnapshot> Fetch() {
            try { await Ensure().ConfigureAwait(false); return GptSnapshot.Parse(await Request("account/rateLimits/read",new {}).ConfigureAwait(false)); }
            catch { StopProcess();throw; }
        }
        public async Task<ResetResult> ConsumeReset(string idempotencyKey) {
            Guid key;if(!Guid.TryParse(idempotencyKey,out key))throw new ArgumentException("Invalid reset request ID");
            try {
                await Ensure().ConfigureAwait(false);
                return ResetResult.Parse(await Request("account/rateLimitResetCredit/consume",new { idempotencyKey=idempotencyKey }).ConfigureAwait(false));
            } catch { StopProcess();throw; }
        }
        void StopProcess() {
            Process p;
            lock(gate) {
                p=process;process=null;
                foreach(var t in pending.Values) t.TrySetException(new ProviderException("Codex 连接已关闭"));
                pending.Clear();
            }
            if(p!=null) { try { p.StandardInput.Close();if(!p.WaitForExit(1500))p.Kill(); } catch {} finally {p.Dispose();} }
        }
        public void Dispose() { disposed=true;StopProcess(); }
    }
}
