using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TokenMonitor {
    public sealed class AvailableRelease {
        public Version Version;
        public string Tag,Notes,AssetHash;
        public string AssetUrl {get{return UpdateClient.ReleasesUrl+"/download/"+Tag+"/TokenMonitor-v"+Version.ToString(3)+"-windows-x64.zip";}}
        public string Url { get { return UpdateClient.ReleasesUrl+"/tag/"+Tag; } }
    }
    public sealed class UpdateClient : IDisposable {
        public const string ReleasesUrl="https://github.com/Ricki0613/TokenMonitor/releases";
        public const string Endpoint="https://api.github.com/repos/Ricki0613/TokenMonitor/releases/latest";
        public static readonly Version Current=typeof(UpdateClient).Assembly.GetName().Version;
        readonly HttpClient http;
        public UpdateClient():this(new HttpClientHandler {AllowAutoRedirect=false}) {}
        internal UpdateClient(HttpMessageHandler handler) {
            http=new HttpClient(handler) {Timeout=TimeSpan.FromSeconds(15),MaxResponseContentBufferSize=2097152};
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenMonitor/"+Current.ToString(3));
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            http.DefaultRequestHeaders.Add("X-GitHub-Api-Version","2022-11-28");
        }
        public static AvailableRelease Parse(string body) {
            var obj=Json.Read(body);
            if(!(Json.Get(obj,"draft") is bool)||!(Json.Get(obj,"prerelease") is bool))throw new InvalidDataException();
            if((bool)Json.Get(obj,"draft")||(bool)Json.Get(obj,"prerelease"))return null;
            string tag=Json.Str(obj,"tag_name");Version version;
            if(tag==null||!Regex.IsMatch(tag,@"\Av?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z")||
                !Version.TryParse(tag.TrimStart('v')+".0",out version))throw new InvalidDataException();
            // Construct the URL ourselves; never open a URL supplied by a remote response.
            var release=new AvailableRelease {Tag=tag,Version=version,Notes=Json.Str(obj,"body")};
            if(release.Notes!=null&&release.Notes.Length>5000)release.Notes=release.Notes.Substring(0,5000);
            var assets=Json.Get(obj,"assets") as System.Collections.IEnumerable;
            if(assets!=null)foreach(var raw in assets){var asset=Json.Object(raw);if(Json.Str(asset,"name")=="TokenMonitor-v"+version.ToString(3)+"-windows-x64.zip") {string digest=Json.Str(asset,"digest");if(digest!=null&&Regex.IsMatch(digest,@"\Asha256:[0-9a-fA-F]{64}\z"))release.AssetHash=digest.Substring(7);}}
            return release;
        }
        public async Task<AvailableRelease> Fetch() {
            using(var response=await http.GetAsync(Endpoint).ConfigureAwait(false)) {
                if(response.StatusCode==HttpStatusCode.Forbidden||(int)response.StatusCode==429)
                    throw new InvalidOperationException("GitHub 暂时限制请求，请稍后重试或打开发布页。");
                if(response.StatusCode==HttpStatusCode.NotFound)
                    throw new InvalidOperationException("暂时无法读取正式发布版本，请打开发布页确认。");
                response.EnsureSuccessStatusCode();
                return Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }
        public void Dispose() {http.Dispose();}
    }
    public sealed partial class MonitorApp {
        readonly UpdateClient updates=new UpdateClient();
        DispatcherTimer updateTimer;
        DateTimeOffset nextUpdate=DateTimeOffset.MinValue,lastUpdate=DateTimeOffset.MinValue;
        bool updateBusy;
        string notifiedTag;
        internal AvailableRelease AvailableUpdate;
        void StartUpdates() {
            if(smoke||offline)return;
            updateTimer=new DispatcherTimer {Interval=TimeSpan.FromMinutes(1)};
            updateTimer.Tick+=async(s,e)=>{if(DateTimeOffset.UtcNow>=nextUpdate)await CheckUpdates(false);};
            updateTimer.Start();
            Dispatcher.BeginInvoke(new Action(async()=>await CheckUpdates(false)),DispatcherPriority.ApplicationIdle);
            tray.BalloonTipClicked+=(s,e)=>Dispatcher.Invoke(new Action(()=>{ShowControl();Main.OpenSettings("关于软件");}));
        }
        public async Task CheckUpdates(bool manual) {
            if(smoke||offline||quitting||updateBusy)return;
            var now=DateTimeOffset.UtcNow;
            if(now-lastUpdate<TimeSpan.FromMinutes(1)) {
                if(manual)Main.UpdateStatus.Text="刚刚已检查，请一分钟后重试。";
                return;
            }
            updateBusy=true;lastUpdate=now;Main.CheckUpdate.IsEnabled=false;
            Main.UpdateStatus.Text="正在检查 GitHub 更新…";
            try {
                var release=await updates.Fetch();
                if(quitting)return;
                if(release==null)throw new InvalidDataException();
                AvailableUpdate=release.Version>UpdateClient.Current?release:null;
                Main.RenderUpdate(AvailableUpdate,AvailableUpdate==null?"当前已是最新版本 v"+UpdateClient.Current.ToString(3):null);
                nextUpdate=DateTimeOffset.UtcNow.AddHours(6);
                if(AvailableUpdate!=null&&notifiedTag!=release.Tag&&tray!=null) {
                    notifiedTag=release.Tag;
                    tray.ShowBalloonTip(6000,"Token Monitor 有可用更新",release.Tag+" 已发布，打开控制面板查看更新。",System.Windows.Forms.ToolTipIcon.Info);
                }
            } catch(Exception e) {
                nextUpdate=DateTimeOffset.UtcNow.AddHours(1);
                if(!quitting)Main.RenderUpdate(AvailableUpdate,e is InvalidOperationException?e.Message:
                    e is TaskCanceledException?"检查更新超时，可稍后重试或打开发布页。":"无法检查更新，可稍后重试或打开发布页。");
            } finally {updateBusy=false;if(!quitting)Main.CheckUpdate.IsEnabled=true;}
        }
        public void OpenUpdate() {UI.Link(AvailableUpdate==null?UpdateClient.ReleasesUrl:AvailableUpdate.Url);}
    }
}
