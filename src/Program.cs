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
using Microsoft.Win32;
using System.Reflection;
using Forms=System.Windows.Forms;

[assembly: AssemblyVersion("1.1.1.0")]
[assembly: AssemblyFileVersion("1.1.1.0")]
[assembly: AssemblyInformationalVersion("1.1.1")]

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
        DispatcherTimer saveTimer;
        Forms.NotifyIcon tray;
        readonly bool smoke;
        readonly string smokeFolder;
        readonly bool offline;
        public MonitorApp(string folder,bool offline=false) {smoke=folder!=null;smokeFolder=folder;this.offline=offline;}
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);ShutdownMode=ShutdownMode.OnExplicitShutdown;
            using(var stream=typeof(MonitorApp).Assembly.GetManifestResourceStream("TokenMonitor.Theme.xaml")) Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
            if(!smoke)LocalStorage.Initialize();
            Prefs=smoke?new Preferences():Preferences.Load();
            Appearance.Apply(Prefs.Appearance);
            if(!smoke)SystemEvents.UserPreferenceChanged+=SystemAppearanceChanged;
            Main=new MainView(this);MainWindow=Main;
            if(!smoke&&!string.IsNullOrEmpty(LocalStorage.Notice))Main.Subtitle.Text="本机配置需要处理，请打开设置查看";
            Gpt=new UsageView(this,true);Deep=new UsageView(this,false);
            Gpt.RestoreSize(Prefs.GptWidth,Prefs.GptHeight,Prefs.GptAutoHeight);Deep.RestoreSize(Prefs.DeepWidth,Prefs.DeepHeight,Prefs.DeepAutoHeight);
            Main.FitHeight();Gpt.FitHeight();Deep.FitHeight();
            var area=SystemParameters.WorkArea;
            Main.Place(Prefs.MainX,Prefs.MainY,area.Left+40,area.Top+65);
            Gpt.Place(Prefs.GptX,Prefs.GptY,Math.Max(area.Left,area.Right-390),area.Top+30);
            bool stack=area.Height>=Gpt.Height+Deep.Height+50;
            Deep.Place(Prefs.DeepX,Prefs.DeepY,Math.Max(area.Left,area.Right-390-(stack?0:370)),stack?area.Top+Gpt.Height+35:area.Top+30);
            Main.Show();if(Prefs.Gpt)Gpt.Show();if(Prefs.Deep)Deep.Show();
            saveTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(400)};
            saveTimer.Tick+=(s,a)=>{saveTimer.Stop();Save();};
            Main.LocationChanged+=(s,a)=>QueueSave();Gpt.LocationChanged+=(s,a)=>QueueSave();Deep.LocationChanged+=(s,a)=>QueueSave();
            Gpt.GeometryChanged+=QueueSave;Deep.GeometryChanged+=QueueSave;
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
        void SystemAppearanceChanged(object sender,UserPreferenceChangedEventArgs e) {
            if(quitting||Prefs.Appearance!="system")return;
            Dispatcher.BeginInvoke(new Action(()=>{if(!quitting&&Prefs.Appearance=="system")Appearance.Apply("system");}));
        }
        void QueueSave() {if(smoke||quitting||saveTimer==null)return;saveTimer.Stop();saveTimer.Start();}
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
            try {GptData=offline?SampleGpt():await codex.Fetch();GptError=null;gptFailures=0;gptCooldown=DateTimeOffset.MinValue;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {GptError=ErrorMessage(e,true);gptFailures++;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,gptFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)gptCooldown=nextGpt;}
            finally {gptBusy=false;if(!quitting){Gpt.Render(GptData,GptError,false);UpdateLabels();}}
        }
        public async Task ResetGptQuota() {
            if(offline||quitting||gptBusy||!Prefs.Gpt)return;
            gptBusy=true;Gpt.UpdateStatus(GptData,GptError,true);
            string message=null;
            try {
                bool retry=!string.IsNullOrEmpty(Prefs.PendingResetKey);
                if(!retry) {
                    GptData=await codex.Fetch();GptError=null;
                    if(GptData.ResetCredits.GetValueOrDefault()<=0) {message="当前没有可用的额度重置次数。";return;}
                }
                string prompt=retry?"确认上次额度重置的结果？\n将复用原请求，避免重复使用重置次数。":
                    "将使用当前 ChatGPT / Codex 账户的 1 次额度重置权益。\n这与在 GPT 中使用重置次数相同，是否继续？";
                if(MessageBox.Show(Gpt,prompt,"额度重置",MessageBoxButton.OKCancel,MessageBoxImage.Question,MessageBoxResult.Cancel)!=MessageBoxResult.OK)return;
                var result=await new ResetRedemption(Prefs,codex.ConsumeReset).Redeem();
                message=result.Success?"额度重置已确认。":result.Outcome=="noCredit"?"官方返回：没有可用的重置次数。":"官方返回：当前没有可重置的额度窗口。";
                try {GptData=await codex.Fetch();GptError=null;gptCooldown=DateTimeOffset.MinValue;gptFailures=0;}
                catch(Exception e) {GptError=ErrorMessage(e,true);message+="\n最新额度暂未同步，请稍后刷新。";}
                nextGpt=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);
            } catch(Exception e) {
                GptError=ErrorMessage(e,true);
                message="未能确认额度重置结果："+GptError+(string.IsNullOrEmpty(Prefs.PendingResetKey)?"":"\n点击“确认上次重置”可安全重试。");
            } finally {
                gptBusy=false;
                if(!quitting){Gpt.Render(GptData,GptError,false);UpdateLabels();if(message!=null)MessageBox.Show(message,"额度重置",MessageBoxButton.OK,MessageBoxImage.Information);}
            }
        }
        public async Task RefreshDeep(bool force) {
            var now=DateTimeOffset.UtcNow;
            if(quitting||deepBusy||!Prefs.Deep||now<deepCooldown||(!force&&now<nextDeep))return;
            deepBusy=true;Deep.UpdateStatus(DeepData,DeepError,true);
            try {DeepData=offline?SampleDeep():await deep.Fetch();DeepError=null;deepFailures=0;deepCooldown=DateTimeOffset.MinValue;nextDeep=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
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
            Prefs.GptWidth=Gpt.Width;Prefs.GptHeight=Gpt.Height;Prefs.GptAutoHeight=Gpt.AutoHeight;
            Prefs.DeepWidth=Deep.Width;Prefs.DeepHeight=Deep.Height;Prefs.DeepAutoHeight=Deep.AutoHeight;
            try {Prefs.Save();}catch(Exception e) {LocalStorage.Notice=LocalStorage.Explain(e,"保存窗口配置");Main.Subtitle.Text="配置保存失败，请打开设置查看";}
        }
        public void Quit() {
            if(quitting)return;Save();quitting=true;
            if(timer!=null)timer.Stop();if(saveTimer!=null)saveTimer.Stop();if(!smoke)SystemEvents.UserPreferenceChanged-=SystemAppearanceChanged;
            if(tray!=null){tray.Visible=false;tray.Dispose();}
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
            if(offline) {
                Gpt.Render(GptData,null,false);Main.FitToContent();Gpt.FitToContent();Deep.FitToContent();
                foreach(string theme in new[]{"light","dark","system"}) {
                    Appearance.Apply(theme);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                    Main.UpdateLayout();Gpt.UpdateLayout();Deep.UpdateLayout();
                    UI.SaveImage(Main,Path.Combine(smokeFolder,"control-"+theme+".png"));
                    UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-"+theme+".png"));
                    UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek-"+theme+".png"));
                    var dialog=new SettingsView(this){Owner=Main};dialog.Show();dialog.FitToContent();dialog.UpdateLayout();UI.SaveImage(dialog,Path.Combine(smokeFolder,"settings-"+theme+".png"));dialog.Exiting=true;dialog.Close();
                    checks[theme+"_ink_updates"]=((System.Windows.Media.SolidColorBrush)Main.GptInfo.Foreground).Color==((System.Windows.Media.SolidColorBrush)Resources["MutedBrush"]).Color;
                }
                Appearance.Apply("light");
                foreach(double width in new[]{300.0,368.0,660.0,940.0}) {
                    Gpt.Width=width;Deep.Width=width;Gpt.FitToContent();Deep.FitToContent();
                    await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);Gpt.UpdateLayout();Deep.UpdateLayout();
                    UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-"+width+".png"));UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek-"+width+".png"));
                    checks["gpt_fits_"+width]=Gpt.Scroll.ExtentHeight<=Gpt.Scroll.ViewportHeight+1;
                    checks["deep_fits_"+width]=Deep.Scroll.ExtentHeight<=Deep.Scroll.ViewportHeight+1;
                    checks["gpt_bottom_gap_"+width]=Math.Abs(Gpt.Scroll.ViewportHeight-Gpt.Scroll.ExtentHeight)<2;
                }
                Gpt.Width=300;Gpt.ResizeBy(0,1,0,-1000);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);Gpt.UpdateLayout();
                checks["short_window_scrolls"]=Gpt.Scroll.ExtentHeight>Gpt.Scroll.ViewportHeight;
                checks["resize_keeps_minimum"]=Gpt.Height==Gpt.MinHeight&&!Gpt.AutoHeight;
                UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-short.png"));Gpt.FitToContent();
                checks["fit_restores_auto_height"]=Gpt.AutoHeight;
                var original=GptData.ResetCredits;GptData.ResetCredits=0;Gpt.Render(GptData,null,false);checks["reset_disabled_without_credit"]=!Gpt.ResetQuota.IsEnabled;
                GptData.ResetCredits=null;Gpt.Render(GptData,null,false);checks["reset_disabled_when_unknown"]=!Gpt.ResetQuota.IsEnabled;
                GptData.ResetCredits=original;Gpt.Render(GptData,null,false);checks["reset_enabled_with_credit"]=Gpt.ResetQuota.IsEnabled;
                Gpt.UpdateStatus(GptData,null,true);checks["reset_disabled_while_busy"]=!Gpt.ResetQuota.IsEnabled;
                Gpt.Render(GptData,"模拟连接失败",false);checks["reset_disabled_on_stale_data"]=!Gpt.ResetQuota.IsEnabled;
                Prefs.PendingResetKey=Guid.NewGuid().ToString();Gpt.Render(GptData,"模拟连接失败",false);checks["uncertain_reset_can_retry"]=Gpt.ResetQuota.IsEnabled;
                Prefs.PendingResetKey=null;Gpt.Render(null,"模拟首次连接失败",false);Gpt.FitToContent();await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-empty.png"));
                checks["no_live_queries_or_reset"]=true;
            }
            File.WriteAllText(Path.Combine(smokeFolder,"smoke.json"),Json.Write(checks));Quit();
        }
        static GptSnapshot SampleGpt() {
            var data=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"planType\":\"plus\",\"primary\":{\"usedPercent\":28,\"windowDurationMins\":300},\"secondary\":{\"usedPercent\":61,\"windowDurationMins\":10080}},\"rateLimitResetCredits\":{\"availableCount\":2}}"));
            data.Groups[0].Windows[0].Reset=DateTimeOffset.UtcNow.AddHours(3);data.Groups[0].Windows[1].Reset=DateTimeOffset.UtcNow.AddDays(4);return data;
        }
        static DeepSnapshot SampleDeep() {return DeepSnapshot.Parse(Json.Read("{\"is_available\":true,\"balance_infos\":[{\"currency\":\"CNY\",\"total_balance\":\"51.7801\",\"granted_balance\":\"1.00\",\"topped_up_balance\":\"50.7801\"},{\"currency\":\"USD\",\"total_balance\":\"0.10\",\"granted_balance\":\"0\",\"topped_up_balance\":\"0.10\"}]}"));}
    }
    public static class Program {
        const string MutexName="Local\\TokenMonitor.Ricki.v1";
        [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hWnd,int cmd);
        [STAThread]
        public static int Main(string[] args) {
            if(args.Length>0&&args[0]=="--self-test") return SelfTests.Run(args.Length>1?args[1]:Path.Combine(Paths.Root,"self-test.json"));
            if(args.Length>0&&args[0]=="--diagnose") return Diagnose(args.Length>1?args[1]:Path.Combine(Paths.Root,"diagnose.json")).GetAwaiter().GetResult();
            if(args.Length>1&&args[0]=="--ui-test") {Paths.TestDataDirectory=Path.Combine(Path.GetFullPath(args[1]),"test-profile");new MonitorApp(args[1],true).Run();return 0;}
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
            LocalStorage.Initialize();
            var result=new Dictionary<string,object>();
            using(var gpt=new CodexProvider())using(var deep=new DeepProvider()) {
                try {var s=await gpt.Fetch();result["gpt"]=new {ok=true,groups=s.Groups.Count,windows=s.Groups.Sum(x=>x.Windows.Count)};}catch(Exception e){result["gpt"]=new {ok=false,error=e.GetType().Name};}
                try {var s=await deep.Fetch();result["deepseek"]=new {ok=true,currencies=s.Balances.Select(x=>x.Currency).ToArray()};}catch(Exception e){result["deepseek"]=new {ok=false,error=e.GetType().Name};}
            }
            File.WriteAllText(report,Json.Write(result));return 0;
        }
    }
}
