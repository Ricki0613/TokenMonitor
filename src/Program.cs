using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace TokenMonitor {
    public sealed class MonitorApp : Application {
        public Preferences Prefs;
        public MainView Main;
        public UsageView Gpt,Deep;
        readonly CodexProvider codex=new CodexProvider();
        readonly DeepProvider deep=new DeepProvider();
        public GptSnapshot GptData;
        public DeepSnapshot DeepData;
        public string GptError,DeepError;
        bool gptBusy,deepBusy,quitting;
        DateTimeOffset nextGpt=DateTimeOffset.MinValue,nextDeep=DateTimeOffset.MinValue;
        DateTimeOffset gptCooldown=DateTimeOffset.MinValue,deepCooldown=DateTimeOffset.MinValue;
        int gptFailures,deepFailures;
        DispatcherTimer timer;
        Forms.NotifyIcon tray;
        readonly bool smoke;
        readonly string smokeFolder;
        public MonitorApp(string folder) {smoke=folder!=null;smokeFolder=folder;}
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);ShutdownMode=ShutdownMode.OnExplicitShutdown;
            using(var stream=typeof(MonitorApp).Assembly.GetManifestResourceStream("TokenMonitor.Theme.xaml")) Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
            Prefs=smoke?new Preferences():Preferences.Load();
            Main=new MainView(this);MainWindow=Main;
            Gpt=new UsageView(this,true);Deep=new UsageView(this,false);
            var area=SystemParameters.WorkArea;
            Main.Place(Prefs.MainX,Prefs.MainY,area.Left+40,area.Top+65);
            Gpt.Place(Prefs.GptX,Prefs.GptY,Math.Max(area.Left,area.Right-390),area.Top+30);
            bool stack=area.Height>=Gpt.Height+Deep.Height+50;
            Deep.Place(Prefs.DeepX,Prefs.DeepY,Math.Max(area.Left,area.Right-390-(stack?0:370)),stack?area.Top+Gpt.Height+35:area.Top+30);
            Main.Show();if(Prefs.Gpt)Gpt.Show();if(Prefs.Deep)Deep.Show();
            Main.LocationChanged+=(s,a)=>Save();Gpt.LocationChanged+=(s,a)=>Save();Deep.LocationChanged+=(s,a)=>Save();
            if(!smoke)MakeTray();
            timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};
            timer.Tick+=async(s,a)=>{
                UpdateLabels();var now=DateTimeOffset.UtcNow;
                if(Prefs.Gpt&&!gptBusy&&now>=nextGpt)await RefreshGpt(false);
                if(Prefs.Deep&&!deepBusy&&now>=nextDeep)await RefreshDeep(false);
            };
            if(!smoke)timer.Start();
            Dispatcher.BeginInvoke(new Action(async()=>{
                await RefreshAll(true);
                if(smoke)await RunSmoke();
            }),DispatcherPriority.ApplicationIdle);
        }
        void MakeTray() {
            var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("打开控制面板",null,(s,e)=>Dispatcher.Invoke(new Action(ShowControl)));
            menu.Items.Add("显示 / 隐藏 GPT",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Main.GptSwitch.IsChecked=!Prefs.Gpt)));
            menu.Items.Add("显示 / 隐藏 DeepSeek",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Main.DeepSwitch.IsChecked=!Prefs.Deep)));
            menu.Items.Add("刷新用量",null,(s,e)=>Dispatcher.Invoke(new Action(async()=>await RefreshAll(true))));
            menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("退出",null,(s,e)=>Dispatcher.Invoke(new Action(Quit)));
            tray=new Forms.NotifyIcon {Text="Token Monitor · GPT / DeepSeek",Icon=new System.Drawing.Icon(Path.Combine(Paths.Root,"TokenMonitor.ico")),Visible=true,ContextMenuStrip=menu};
            tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(ShowControl));
        }
        public void ShowControl() {Main.Show();Main.WindowState=WindowState.Normal;Main.Activate();}
        public void SetGpt(bool show) {Prefs.Gpt=show;if(show){Gpt.Show();RefreshGpt(true);}else Gpt.Hide();Save();UpdateLabels();}
        public void SetDeep(bool show) {Prefs.Deep=show;if(show){Deep.Show();RefreshDeep(true);}else Deep.Hide();Save();UpdateLabels();}
        public void ResetDeepCooldown() {deepCooldown=DateTimeOffset.MinValue;nextDeep=DateTimeOffset.MinValue;deepFailures=0;}
        public async Task RefreshAll(bool force) {
            if(quitting)return;
            Main.Refresh.IsEnabled=false;
            try {await Task.WhenAll(Prefs.Gpt?RefreshGpt(force):Task.FromResult(0),Prefs.Deep?RefreshDeep(force):Task.FromResult(0));}
            finally {if(!quitting)Main.Refresh.IsEnabled=true;}
        }
        static string ErrorMessage(Exception e,bool gpt) {
            if(e is ProviderException||e is InvalidDataException)return e.Message;
            if(e is TimeoutException||e is TaskCanceledException)return "连接超时，稍后自动重试";
            if(e is HttpRequestException)return "网络连接失败，稍后自动重试";
            if(e is CryptographicException)return "无法解密密钥，请在设置中重新保存";
            if(!gpt&&e is InvalidOperationException)return "请在设置中填写 DeepSeek API Key";
            if(e is UnauthorizedAccessException)return "访问被拒绝，请检查安装目录权限";
            return gpt?"连接失败，请确认 Codex 已登录后重试":"查询失败，稍后自动重试";
        }
        static int Backoff(Exception e,int failures,int interval) {
            var p=e as ProviderException;
            return Math.Max(p!=null?p.Cooldown:0,Math.Min(300,interval*(1<<Math.Min(3,failures-1))));
        }
        public async Task RefreshGpt(bool force) {
            var now=DateTimeOffset.UtcNow;
            if(quitting||gptBusy||!Prefs.Gpt||now<gptCooldown||(!force&&now<nextGpt))return;
            gptBusy=true;Gpt.UpdateStatus(GptData,GptError,true);
            try {GptData=await codex.Fetch();GptError=null;gptFailures=0;gptCooldown=DateTimeOffset.MinValue;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {GptError=ErrorMessage(e,true);gptFailures++;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,gptFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)gptCooldown=nextGpt;}
            finally {gptBusy=false;if(!quitting){Gpt.Render(GptData,GptError,false);UpdateLabels();}}
        }
        public async Task RefreshDeep(bool force) {
            var now=DateTimeOffset.UtcNow;
            if(quitting||deepBusy||!Prefs.Deep||now<deepCooldown||(!force&&now<nextDeep))return;
            deepBusy=true;Deep.UpdateStatus(DeepData,DeepError,true);
            try {DeepData=await deep.Fetch();DeepError=null;deepFailures=0;deepCooldown=DateTimeOffset.MinValue;nextDeep=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {DeepError=ErrorMessage(e,false);deepFailures++;nextDeep=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,deepFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)deepCooldown=nextDeep;}
            finally {deepBusy=false;if(!quitting){Deep.Render(DeepData,DeepError,false);UpdateLabels();}}
        }
        public void UpdateLabels() {
            if(Main==null||Gpt==null||Deep==null)return;
            string summary="等待同步";
            if(GptData!=null) {
                var group=GptData.Groups.FirstOrDefault(x=>x.Id=="codex")??GptData.Groups[0];
                summary=string.Join(" · ",group.Windows.Take(2).Select(x=>x.Label.Replace("额度","")+" "+(x.Remaining.HasValue?x.Remaining.Value.ToString("0.#")+"%":"—")))+" 剩余";
            }
            Main.GptInfo.Text=!Prefs.Gpt?"已隐藏 · 暂停刷新":GptError!=null?"连接异常 · 打开悬浮窗查看":summary;
            Main.DeepInfo.Text=!Prefs.Deep?"已隐藏 · 暂停刷新":DeepError!=null?"连接异常 · 打开悬浮窗查看":DeepData!=null?"可用余额 "+string.Join(" / ",DeepData.Balances.Select(x=>x.Format(x.Total))):"等待同步";
            Gpt.UpdateStatus(GptData,GptError,gptBusy);Deep.UpdateStatus(DeepData,DeepError,deepBusy);
        }
        public void Save() {
            if(smoke||quitting||Main==null||Gpt==null||Deep==null)return;
            Prefs.MainX=Main.Left;Prefs.MainY=Main.Top;Prefs.GptX=Gpt.Left;Prefs.GptY=Gpt.Top;Prefs.DeepX=Deep.Left;Prefs.DeepY=Deep.Top;
            try {Prefs.Save();}catch {Main.Subtitle.Text="位置保存失败，请检查目录权限";}
        }
        public void Quit() {
            if(quitting)return;Save();quitting=true;
            if(timer!=null)timer.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();}
            codex.Dispose();deep.Dispose();Main.Exiting=Gpt.Exiting=Deep.Exiting=true;Shutdown();
        }
        async Task RunSmoke() {
            Directory.CreateDirectory(smokeFolder);
            var checks=new Dictionary<string,object>();
            checks["gpt_live"]=GptData!=null;checks["deepseek_live"]=DeepData!=null;
            checks["gpt_error"]=GptError;checks["deepseek_error"]=DeepError;
            Main.GptSwitch.IsChecked=false;checks["gpt_switch_hides_only_gpt"]=!Gpt.IsVisible&&Deep.IsVisible;
            Main.GptSwitch.IsChecked=true;Main.DeepSwitch.IsChecked=false;checks["deepseek_switch_hides_only_deepseek"]=Gpt.IsVisible&&!Deep.IsVisible;
            Main.DeepSwitch.IsChecked=true;checks["floating_topmost"]=Gpt.Topmost&&Deep.Topmost;
            Gpt.Close();checks["close_gpt_updates_switch"]=Main.GptSwitch.IsChecked==false&&!Gpt.IsVisible;
            Main.GptSwitch.IsChecked=true;Main.Hide();checks["controller_can_hide"]=!Main.IsVisible&&Gpt.IsVisible&&Deep.IsVisible;ShowControl();
            await Task.Delay(1500);
            Main.UpdateLayout();Gpt.UpdateLayout();Deep.UpdateLayout();
            checks["main_content_fits"]=Main.Scroll.ExtentHeight<=Main.Scroll.ViewportHeight+0.5;
            checks["gpt_content_fits"]=Gpt.Scroll.ExtentHeight<=Gpt.Scroll.ViewportHeight+0.5;
            checks["deep_content_fits"]=Deep.Scroll.ExtentHeight<=Deep.Scroll.ViewportHeight+0.5;
            UI.SaveImage(Main,""+Path.Combine(smokeFolder,"control.png"));
            UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt.png"));
            UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek.png"));
            if(GptData!=null) {
                Gpt.Render(GptData,"网络连接失败，稍后自动重试",false);Gpt.UpdateLayout();UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-stale.png"));
            }
            var settings=new SettingsView(this){Owner=Main};settings.Show();settings.UpdateLayout();UI.SaveImage(settings,Path.Combine(smokeFolder,"settings.png"));settings.Exiting=true;settings.Close();
            File.WriteAllText(Path.Combine(smokeFolder,"smoke.json"),Json.Write(checks));Quit();
        }
    }
    public static class Program {
        const string MutexName="Local\\TokenMonitor.Ricki.v1";
        [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hWnd,int cmd);
        [STAThread]
        public static int Main(string[] args) {
            if(args.Length>0&&args[0]=="--self-test") return SelfTests.Run(args.Length>1?args[1]:Path.Combine(Paths.Root,"self-test.json"));
            if(args.Length>0&&args[0]=="--diagnose") return Diagnose(args.Length>1?args[1]:Path.Combine(Paths.Root,"diagnose.json")).GetAwaiter().GetResult();
            bool fresh;using(var mutex=new Mutex(true,MutexName,out fresh)) {
                if(!fresh) {
                    // Use a named event instead of window titles: it also works when the control panel is hidden.
                    try {using(var signal=EventWaitHandle.OpenExisting("Local\\TokenMonitor.Show.v1"))signal.Set();}catch{}
                    return 0;
                }
                using(var signal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\TokenMonitor.Show.v1")) {
                    var app=new MonitorApp(args.Length>1&&args[0]=="--smoke-test"?args[1]:null);
                    var registration=ThreadPool.RegisterWaitForSingleObject(signal,(state,timedOut)=>app.Dispatcher.BeginInvoke(new Action(app.ShowControl)),null,Timeout.Infinite,false);
                    try {app.Run();return 0;}catch {MessageBox.Show("启动失败，请检查安装文件是否完整，或重新安装 Token Monitor。","Token Monitor",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
                    finally {registration.Unregister(null);mutex.ReleaseMutex();}
                }
            }
        }
        static async Task<int> Diagnose(string report) {
            var result=new Dictionary<string,object>();
            using(var gpt=new CodexProvider())using(var deep=new DeepProvider()) {
                try {var s=await gpt.Fetch();result["gpt"]=new {ok=true,groups=s.Groups.Count,windows=s.Groups.Sum(x=>x.Windows.Count)};}catch(Exception e){result["gpt"]=new {ok=false,error=e.GetType().Name};}
                try {var s=await deep.Fetch();result["deepseek"]=new {ok=true,currencies=s.Balances.Select(x=>x.Currency).ToArray()};}catch(Exception e){result["deepseek"]=new {ok=false,error=e.GetType().Name};}
            }
            File.WriteAllText(report,Json.Write(result));return 0;
        }
    }
}
