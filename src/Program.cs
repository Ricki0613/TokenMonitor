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

[assembly: AssemblyVersion("4.1.0.0")]
[assembly: AssemblyFileVersion("4.1.0.0")]
[assembly: AssemblyInformationalVersion("4.1.0")]

namespace TokenMonitor {
    public sealed partial class MonitorApp : Application {
        public Preferences Prefs;
        public bool FirstRun;
        public bool MonitoringGpt {get{return Prefs.MonitorGpt??Prefs.Gpt;}}
        public bool MonitoringDeep {get{return Prefs.MonitorDeep??Prefs.Deep;}}
        public bool MonitoringOpenAi {get{return Prefs.MonitorOpenAi??Prefs.OpenAi;}}
        public MainView Main;
        public CompactView Gpt,Deep,OpenAi,KimiWindow,KimiApiWindow;
        public OrbView KimiOrb;
        QuotaWindow lastKimiQuota;
        public OrbView Orb;
        readonly CodexProvider codex=new CodexProvider();
        readonly DeepProvider deep=new DeepProvider();
        readonly OpenAiProvider openAi=new OpenAiProvider();
        public GptSnapshot GptData;
        public DeepSnapshot DeepData;
        public OpenAiSnapshot OpenAiData;
        public string OpenAiError;
        bool openAiBusy;
        public bool OpenAiKeyValidated;
        int openAiFailures;
        DateTimeOffset nextOpenAi=DateTimeOffset.MinValue,openAiCooldown=DateTimeOffset.MinValue;
        public string GptError,DeepError;
        bool gptBusy,deepBusy,quitting,changingStorage;
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
            FirstRun=!smoke&&!File.Exists(Paths.Preferences);
            if(FirstRun){Prefs.Gpt=false;Prefs.Deep=false;Prefs.OpenAi=false;}
            Prefs.MonitorGpt=MonitoringGpt;Prefs.MonitorDeep=MonitoringDeep;Prefs.MonitorOpenAi=MonitoringOpenAi;
            if(offline){Prefs.OpenAi=true;Prefs.MonitorOpenAi=true;}
            Appearance.Apply(Prefs.Appearance);
            FontSizing.Apply(Prefs.FontSize);
            if(!smoke)SystemEvents.UserPreferenceChanged+=SystemAppearanceChanged;
            Main=new MainView(this);MainWindow=Main;
            if(!smoke&&!string.IsNullOrEmpty(LocalStorage.Notice))Main.Subtitle.Text="本机配置需要处理，请打开设置查看";
            Gpt=new CompactView("ChatGPT",0,()=>RefreshGpt(true),()=>Main.GptSwitch.IsChecked=false);
            Deep=new CompactView("DeepSeek",1,()=>RefreshDeep(true),()=>Main.DeepSwitch.IsChecked=false);
            OpenAi=new CompactView("OpenAI API",2,()=>RefreshOpenAi(true),()=>Main.OpenAiSwitch.IsChecked=false);
            KimiWindow=new CompactView("Kimi 订阅",3,()=>UpdateKUsage(),()=>SetKimiVisible(false,false));
            KimiApiWindow=new CompactView("Kimi API",4,()=>UpdateKBalance(),()=>SetKimiVisible(false,true));
            foreach(var floating in new[]{Gpt,Deep,OpenAi,KimiWindow,KimiApiWindow})floating.ConfigureMenu(this);
            Gpt.SwitchDisplay=()=>SetOrbMode(true);KimiWindow.SwitchDisplay=()=>SetKimiOrbMode(true);
            ApplyFloatOpacity(Prefs.FloatOpacity);
            Orb=new OrbView(this);Orb.Place();Orb.LocationChanged+=(s,a)=>QueueSave();
            KimiOrb=new OrbView(this,true);KimiOrb.Place();KimiOrb.LocationChanged+=(s,a)=>QueueSave();
            KimiWindow.GeometryChanged+=QueueSave;KimiApiWindow.GeometryChanged+=QueueSave;
            KimiWindow.LocationChanged+=(s,a)=>QueueSave();KimiApiWindow.LocationChanged+=(s,a)=>QueueSave();
            OpenAi.RestoreSize(Prefs.OpenAiWidth,Prefs.OpenAiHeight,Prefs.OpenAiAutoHeight);
            Gpt.RestoreSize(Prefs.GptWidth,Prefs.GptHeight,Prefs.GptAutoHeight);Deep.RestoreSize(Prefs.DeepWidth,Prefs.DeepHeight,Prefs.DeepAutoHeight);
            Main.FitHeight();Gpt.FitHeight();Deep.FitHeight();OpenAi.FitHeight();
            var area=SystemParameters.WorkArea;
            Main.Place(Prefs.MainX,Prefs.MainY,area.Left+40,area.Top+65);
            Gpt.Place(Prefs.GptX,Prefs.GptY,Math.Max(area.Left,area.Right-390),area.Top+30);
            bool stack=area.Height>=Gpt.Height+Deep.Height+50;
            Deep.Place(Prefs.DeepX,Prefs.DeepY,Math.Max(area.Left,area.Right-390-(stack?0:370)),stack?area.Top+Gpt.Height+35:area.Top+30);
            OpenAi.Place(Prefs.OpenAiX,Prefs.OpenAiY,Math.Max(area.Left,area.Right-770),area.Top+30);
            KimiWindow.Place(Prefs.KimiX,Prefs.KimiY,area.Right-250,area.Top+220);
            KimiApiWindow.Place(Prefs.KimiApiX,Prefs.KimiApiY,area.Right-250,area.Top+340);
            if(Prefs.KimiFloat){if(Prefs.KimiOrbMode)KimiOrb.Show();else KimiWindow.Show();}
            if(Prefs.KimiApiFloat)KimiApiWindow.Show();
            Main.Show();if(Prefs.Gpt){if(Prefs.OrbMode)Orb.Show();else Gpt.Show();}if(Prefs.Deep)Deep.Show();if(Prefs.OpenAi)OpenAi.Show();
            saveTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(400)};
            saveTimer.Tick+=(s,a)=>{saveTimer.Stop();Save();};
            Main.LocationChanged+=(s,a)=>QueueSave();Gpt.LocationChanged+=(s,a)=>QueueSave();Deep.LocationChanged+=(s,a)=>QueueSave();
            Gpt.GeometryChanged+=QueueSave;Deep.GeometryChanged+=QueueSave;
            OpenAi.LocationChanged+=(s,a)=>QueueSave();OpenAi.GeometryChanged+=QueueSave;
            if(!smoke)MakeTray();
            StartUpdates();
            timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};
            timer.Tick+=async(s,a)=>{
                UpdateLabels();var now=DateTimeOffset.UtcNow;
                if(now>=nextKimi)await RefreshKimi(); if(MonitoringGpt&&!gptBusy&&now>=nextGpt)await RefreshGpt(false);
                if(MonitoringDeep&&!deepBusy&&now>=nextDeep)await RefreshDeep(false);
                if(MonitoringOpenAi&&!openAiBusy&&now>=nextOpenAi)await RefreshOpenAi(false);
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
        internal void ApplyFloatOpacity(int percent) {
            Prefs.FloatOpacity=Math.Max(0,Math.Min(100,percent));
            foreach(var window in new[]{Gpt,Deep,OpenAi,KimiWindow,KimiApiWindow})
                window.Opacity=0.45+0.55*Prefs.FloatOpacity/100.0;
            QueueSave();
        }
        void QueueSave() {if(smoke||quitting||saveTimer==null)return;saveTimer.Stop();saveTimer.Start();}
        void MakeTray() {
            var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("打开控制面板",null,(s,e)=>Dispatcher.Invoke(new Action(ShowControl)));
            menu.Items.Add("显示 / 隐藏 GPT",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Main.GptSwitch.IsChecked=!Prefs.Gpt)));
            menu.Items.Add("显示 / 隐藏 DeepSeek",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Main.DeepSwitch.IsChecked=!Prefs.Deep)));
            menu.Items.Add("显示 / 隐藏 Kimi 订阅",null,(s,e)=>Dispatcher.Invoke(new Action(()=>SetKimiVisible(!Prefs.KimiFloat,false))));
            menu.Items.Add("显示 / 隐藏 Kimi API",null,(s,e)=>Dispatcher.Invoke(new Action(()=>SetKimiVisible(!Prefs.KimiApiFloat,true))));
            menu.Items.Add("显示 / 隐藏 OpenAI API",null,(s,e)=>Dispatcher.Invoke(new Action(()=>Main.OpenAiSwitch.IsChecked=!Prefs.OpenAi)));
            menu.Items.Add("检查更新",null,(s,e)=>Dispatcher.Invoke(new Action(async()=>{ShowControl();Main.OpenSettings("关于软件");await CheckUpdates(true);})));
            menu.Items.Add("刷新用量",null,(s,e)=>Dispatcher.Invoke(new Action(async()=>await RefreshAll(true))));
            menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("退出",null,(s,e)=>Dispatcher.Invoke(new Action(Quit)));
            tray=new Forms.NotifyIcon {Text="Token Monitor · ChatGPT / DeepSeek / Kimi / OpenAI",Icon=new System.Drawing.Icon(Path.Combine(Paths.Root,"TokenMonitor.ico")),Visible=true,ContextMenuStrip=menu};
            tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(ShowControl));
        }
        public void ShowControl() {Main.Show();Main.WindowState=WindowState.Normal;Main.Activate();}
        public void SetGpt(bool show) {Prefs.Gpt=show;if(show){Prefs.MonitorGpt=true;if(Prefs.OrbMode)Orb.Show();else Gpt.Show();RefreshGpt(true);}else {Gpt.Hide();Orb.Hide();}Save();UpdateLabels();}
        public void SetOrbMode(bool compact) {
            Prefs.OrbMode=compact;
            Gpt.Hide();Orb.Hide();
            if(Prefs.Gpt){if(compact){Orb.ApplyScale();Orb.Show();}else {Gpt.Show();Gpt.Activate();}}
            Save();UpdateLabels();
        }
        public void SetKimiVisible(bool show,bool api) {
            if(api){
                Prefs.KimiApiFloat=show;
                Main.KimiApiSwitch.IsChecked=show;
                if(show)KimiApiWindow.Show();else KimiApiWindow.Hide();
            } else {
                Prefs.KimiFloat=show;
                Main.KimiSwitch.IsChecked=show;
                KimiWindow.Hide();KimiOrb.Hide();
                if(show){if(Prefs.KimiOrbMode)KimiOrb.Show();else KimiWindow.Show();}
            }
            Save();
        }
        public void SetKimiOrbMode(bool compact){
            Prefs.KimiOrbMode=compact;
            SetKimiVisible(true,false);
        }
        internal void DisplayKimiUsage(QuotaWindow reading,string status){
            if(reading!=null)lastKimiQuota=reading;
            KimiWindow.Render(lastKimiQuota,reading==null?status:null,false);
            KimiOrb.RenderKimi(lastKimiQuota,reading==null?status:null);
        }
        internal void DisplayKimiBalance(KimiBalance reading){
            KimiApiWindow.Render(reading,reading.Amount.HasValue?null:reading.Status,false);
        }
        public void SetDeep(bool show) {Prefs.Deep=show;if(show){Prefs.MonitorDeep=true;Deep.Show();RefreshDeep(true);}else Deep.Hide();Save();UpdateLabels();}
        public void ResetDeepCooldown() {deepCooldown=DateTimeOffset.MinValue;nextDeep=DateTimeOffset.MinValue;deepFailures=0;}
        public void SetOpenAi(bool show) {Prefs.OpenAi=show;if(show){Prefs.MonitorOpenAi=true;OpenAi.Show();RefreshOpenAi(true);}else OpenAi.Hide();Save();UpdateLabels();}
        public void ResetOpenAiCooldown() {openAiCooldown=DateTimeOffset.MinValue;nextOpenAi=DateTimeOffset.MinValue;openAiFailures=0;}
        public async Task VerifyOpenAiKey(string key) {
            if(quitting||changingStorage||openAiBusy)throw new ProviderException("正在连接，请稍后重试。");
            openAiBusy=true;
            try {
                await openAi.ValidateApiKey(key);
                OpenAiConnection previous=null;
                try {if(File.Exists(Paths.OpenAiKey))previous=OpenAiSecretStore.Read();}catch { }
                bool same=previous!=null&&previous.ApiKey==key;
                var connection=same?previous:new OpenAiConnection {ApiKey=key};
                OpenAiSecretStore.Save(connection);
                if(!same)OpenAiData=null;
                OpenAiError=OpenAiData==null?OpenAiConnectionHelp.Next(connection):null;
                Prefs.MonitorOpenAi=true;Save();ResetOpenAiCooldown();
            } finally {openAiBusy=false;UpdateLabels();}
        }
        public async Task ConnectOpenAi(OpenAiConnection connection) {
            OpenAiKeyValidated=false;
            if(quitting||changingStorage)throw new ProviderException("请等待数据目录切换完成后再登录。");
            OpenAiSecretStore.Validate(connection);
            // Keep the old connection and snapshot until validation and persistence succeed.
            while(openAiBusy) {if(quitting)throw new OperationCanceledException();await Task.Delay(100);}
            if(quitting||changingStorage)throw new ProviderException("请等待数据目录切换完成后再登录。");
            openAiBusy=true;
            OpenAiSnapshot snapshot=null;
            try {
                await openAi.ValidateApiKey(connection.ApiKey);OpenAiKeyValidated=true;
                if(!string.IsNullOrEmpty(connection.AdminKey)&&!string.IsNullOrEmpty(connection.ApiKeyId))snapshot=await openAi.Fetch(connection);
                OpenAiSecretStore.Save(connection);
                OpenAiData=snapshot;if(snapshot!=null)RecordHistory(snapshot);
                OpenAiError=snapshot==null?OpenAiConnectionHelp.Next(connection):null;
                ResetOpenAiCooldown();
                nextOpenAi=DateTimeOffset.UtcNow.AddSeconds(snapshot==null?300:Prefs.Interval);
                if(snapshot==null)openAiCooldown=nextOpenAi;
                Prefs.MonitorOpenAi=true;Save();
            } finally {
                openAiBusy=false;
                if(!quitting){OpenAi.Render(OpenAiData,OpenAiError,false);UpdateLabels();}
            }
        }
        DateTimeOffset nextKimi;
        bool kimiBusy;
        async Task RefreshKimi(){
            if(offline||kimiBusy||changingStorage)return;
            kimiBusy=true;
            try {await Task.WhenAll(UpdateKUsage(),UpdateKBalance());}
            finally {nextKimi=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);kimiBusy=false;}
        }
        internal async Task<string> UpdateKUsage() {
            return Main.ShowKimiUsage(await Kimi.Usage());
        }
        internal async Task<string> UpdateKBalance(string key=null,bool save=false) {
            return Main.ShowKimiBalance(await Kimi.Balance(key,save));
        }
        public async Task RefreshAll(bool force) {
            if(quitting)return;
            Main.Refresh.IsEnabled=false;
            try {await Task.WhenAll(RefreshKimi(),MonitoringGpt?RefreshGpt(force):Task.FromResult(0),MonitoringDeep?RefreshDeep(force):Task.FromResult(0),MonitoringOpenAi?RefreshOpenAi(force):Task.FromResult(0));}
            finally {if(!quitting)Main.Refresh.IsEnabled=true;}
        }
        static string ErrorMessage(Exception e,bool gpt) {
            if(e is ProviderException||e is InvalidDataException)return e.Message;
            if(e is TimeoutException||e is TaskCanceledException)return "连接超时，稍后自动重试";
            if(e is HttpRequestException)return "网络连接失败，稍后自动重试";
            if(e is CryptographicException)return "无法解密密钥，请在设置中重新保存";
            if(!gpt&&e is InvalidOperationException)return "请在设置中填写 DeepSeek API Key";
            if(e is UnauthorizedAccessException)return "访问被拒绝，请在设置中检查数据目录权限";
            return gpt?"连接失败，请确认 Codex 已登录后重试":"查询失败，稍后自动重试";
        }
        static int Backoff(Exception e,int failures,int interval) {
            var p=e as ProviderException;
            return Math.Max(p!=null?p.Cooldown:0,Math.Min(300,interval*(1<<Math.Min(3,failures-1))));
        }
        public async Task RefreshGpt(bool force) {
            var now=DateTimeOffset.UtcNow;
            if(quitting||changingStorage||gptBusy||!MonitoringGpt||now<gptCooldown||(!force&&now<nextGpt))return;
            gptBusy=true;Gpt.UpdateStatus(GptData,GptError,true);
            try {GptData=offline?SampleGpt():await codex.Fetch();RecordHistory(GptData);GptError=null;gptFailures=0;gptCooldown=DateTimeOffset.MinValue;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {GptError=ErrorMessage(e,true);gptFailures++;nextGpt=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,gptFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)gptCooldown=nextGpt;}
            finally {gptBusy=false;if(!quitting){Gpt.Render(GptData,GptError,false);UpdateLabels();}}
        }
        public async Task ResetGptQuota() {
            if(offline||quitting||changingStorage||gptBusy||!MonitoringGpt)return;
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
            if(quitting||changingStorage||deepBusy||!MonitoringDeep||now<deepCooldown||(!force&&now<nextDeep))return;
            string requestedSource=Prefs.DeepSource;
            deepBusy=true;Deep.UpdateStatus(DeepData,DeepError,true);
            try {DeepData=offline?SampleDeep():await deep.Fetch(Prefs.DeepSource);RecordHistory(DeepData);DeepError=null;deepFailures=0;deepCooldown=DateTimeOffset.MinValue;nextDeep=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {DeepError=ErrorMessage(e,false);deepFailures++;nextDeep=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,deepFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)deepCooldown=nextDeep;}
            finally {if(requestedSource!=Prefs.DeepSource){DeepData=null;DeepError=null;ResetDeepCooldown();}deepBusy=false;if(!quitting){Deep.Render(DeepData,DeepError,false);UpdateLabels();}}
        }
        public void UpdateLabels() {
            if(Main==null||Gpt==null||Deep==null||OpenAi==null)return;
            Orb.Render(GptData,GptError,gptBusy);
            bool retry=!string.IsNullOrEmpty(Prefs.PendingResetKey);
            string count=GptData!=null&&GptData.ResetCredits.HasValue?GptData.ResetCredits.Value.ToString():"—";
            Main.ResetQuota.Content=retry?"确认上次重置":"重置 ("+count+")";
            Main.ResetQuota.ToolTip=count=="—"?"尚未获知可重置次数":"剩余可重置 "+count+" 次";
            Main.ResetQuota.IsEnabled=!gptBusy&&(retry||(GptError==null&&GptData!=null&&GptData.ResetCredits.GetValueOrDefault()>0));
            Main.GptInfo.Text=GptError!=null?(GptData!=null?"旧数据 · ":"")+GptError:GptData!=null?"已同步 "+UI.Clock(GptData.Fetched)+" · 剩余额度":"选择连接设置，连接本机账号";
            Main.DeepInfo.Text=DeepError!=null?(DeepData!=null?"旧数据 · ":"")+DeepError:DeepData!=null?"已同步 "+UI.Clock(DeepData.Fetched)+(DeepData.FromHarness?" · Harness":" · API"):"选择连接设置，添加余额来源";
            Main.OpenAiInfo.Text=OpenAiError!=null?(OpenAiData!=null?"旧数据 · ":"")+OpenAiError:OpenAiData!=null?"历史用量已连接 · "+UI.Clock(OpenAiData.Fetched):"选择连接设置，接入历史用量";
            Main.RenderMetrics();
            Gpt.UpdateStatus(GptData,GptError,gptBusy);Deep.UpdateStatus(DeepData,DeepError,deepBusy);
            OpenAi.UpdateStatus(OpenAiData,OpenAiError,openAiBusy);
        }
        public async Task RefreshOpenAi(bool force) {
            var now=DateTimeOffset.UtcNow;
            if(quitting||changingStorage||openAiBusy||!MonitoringOpenAi||now<openAiCooldown||(!force&&now<nextOpenAi))return;
            openAiBusy=true;OpenAi.UpdateStatus(OpenAiData,OpenAiError,true);
            try {OpenAiData=offline?SampleOpenAi():await openAi.Fetch(OpenAiSecretStore.Read());RecordHistory(OpenAiData);OpenAiError=null;openAiFailures=0;openAiCooldown=DateTimeOffset.MinValue;nextOpenAi=DateTimeOffset.UtcNow.AddSeconds(Prefs.Interval);}
            catch(Exception e) {OpenAiError=e is InvalidOperationException?"OpenAI 配置不可用，请在设置中重新登录。":ErrorMessage(e,false);openAiFailures++;nextOpenAi=DateTimeOffset.UtcNow.AddSeconds(Backoff(e,openAiFailures,Prefs.Interval));var p=e as ProviderException;if(p!=null&&p.Cooldown>0)openAiCooldown=nextOpenAi;}
            finally {openAiBusy=false;if(!quitting){OpenAi.Render(OpenAiData,OpenAiError,false);UpdateLabels();}}
        }
        public void Save() {
            if(smoke||quitting||Main==null||Gpt==null||Deep==null||OpenAi==null)return;
            Prefs.KimiX=KimiWindow.Left;Prefs.KimiY=KimiWindow.Top;Prefs.KimiWidth=KimiWindow.Width;
            Prefs.KimiApiX=KimiApiWindow.Left;Prefs.KimiApiY=KimiApiWindow.Top;Prefs.KimiApiWidth=KimiApiWindow.Width;
            Prefs.KimiOrbX=KimiOrb.Left;Prefs.KimiOrbY=KimiOrb.Top;
            Prefs.OrbX=Orb.Left;Prefs.OrbY=Orb.Top;
            Prefs.MainX=Main.WindowState==WindowState.Normal?Main.Left:Main.RestoreBounds.Left;Prefs.MainY=Main.WindowState==WindowState.Normal?Main.Top:Main.RestoreBounds.Top;Prefs.GptX=Gpt.Left;Prefs.GptY=Gpt.Top;Prefs.DeepX=Deep.Left;Prefs.DeepY=Deep.Top;
            Prefs.GptWidth=Gpt.Width;Prefs.GptHeight=Gpt.Height;Prefs.GptAutoHeight=Gpt.AutoHeight;
            Prefs.DeepWidth=Deep.Width;Prefs.DeepHeight=Deep.Height;Prefs.DeepAutoHeight=Deep.AutoHeight;
            Prefs.OpenAiX=OpenAi.Left;Prefs.OpenAiY=OpenAi.Top;Prefs.OpenAiWidth=OpenAi.Width;Prefs.OpenAiHeight=OpenAi.Height;Prefs.OpenAiAutoHeight=OpenAi.AutoHeight;
            try {Prefs.Save();}catch(Exception e) {LocalStorage.Notice=LocalStorage.Explain(e,"保存窗口配置");Main.Subtitle.Text="配置保存失败，请打开设置查看";}
        }
        public async Task ChangeDataDirectory(string destination) {
            if(changingStorage)throw new InvalidOperationException("已有数据目录切换正在进行");
            changingStorage=true;
            try {
                while(gptBusy||deepBusy||openAiBusy) {if(quitting)throw new OperationCanceledException();await Task.Delay(100);}
                if(quitting)throw new OperationCanceledException();
                Save();StorageLocation.Relocate(destination,Prefs);
                Main.Subtitle.Text="用量，随时可见  ·  v4.1.0";
            } finally {changingStorage=false;}
        }
        public void Quit() {
            if(quitting)return;Save();quitting=true;
            if(updateTimer!=null)updateTimer.Stop();updates.Dispose();
            if(timer!=null)timer.Stop();if(saveTimer!=null)saveTimer.Stop();if(!smoke)SystemEvents.UserPreferenceChanged-=SystemAppearanceChanged;
            if(tray!=null){tray.Visible=false;tray.Dispose();}
            KimiWindow.Exiting=KimiApiWindow.Exiting=KimiOrb.Exiting=Orb.Exiting=true;codex.Dispose();deep.Dispose();openAi.Dispose();Main.Exiting=Gpt.Exiting=Deep.Exiting=OpenAi.Exiting=true;Shutdown();
        }
        async Task RunSmoke() {
            Directory.CreateDirectory(smokeFolder);
            var checks=new Dictionary<string,object>();
            checks["gpt_live"]=GptData!=null;checks["deepseek_live"]=DeepData!=null;
            checks["gpt_error"]=GptError;checks["deepseek_error"]=DeepError;
            if(offline)checks["openai_sample_loaded"]=OpenAiData!=null&&OpenAiError==null;
            Main.GptSwitch.IsChecked=false;checks["gpt_switch_hides_only_gpt"]=!Gpt.IsVisible&&Deep.IsVisible;
            Main.GptSwitch.IsChecked=true;Main.DeepSwitch.IsChecked=false;checks["deepseek_switch_hides_only_deepseek"]=Gpt.IsVisible&&!Deep.IsVisible;
            Main.DeepSwitch.IsChecked=true;checks["floating_topmost"]=Gpt.Topmost&&Deep.Topmost;
            if(offline) {
                Main.OpenAiSwitch.IsChecked=false;checks["openai_switch_hides_only_openai"]=!OpenAi.IsVisible&&Gpt.IsVisible&&Deep.IsVisible;
                Main.OpenAiSwitch.IsChecked=true;checks["openai_topmost"]=OpenAi.IsVisible&&OpenAi.Topmost;
                OpenAi.Close();checks["close_openai_updates_switch"]=Main.OpenAiSwitch.IsChecked==false&&!OpenAi.IsVisible;
                Main.OpenAiSwitch.IsChecked=true;
            }
            Gpt.Close();checks["close_gpt_updates_switch"]=Main.GptSwitch.IsChecked==false&&!Gpt.IsVisible;
            Main.GptSwitch.IsChecked=true;Main.Hide();checks["controller_can_hide"]=!Main.IsVisible&&Gpt.IsVisible&&Deep.IsVisible;ShowControl();
            await Task.Delay(1500);
            Main.UpdateLayout();Gpt.UpdateLayout();Deep.UpdateLayout();OpenAi.UpdateLayout();
            checks["main_content_fits"]=Main.Scroll.ExtentHeight<=Main.Scroll.ViewportHeight+0.5;
            checks["gpt_content_fits"]=Gpt.Scroll.ExtentHeight<=Gpt.Scroll.ViewportHeight+0.5;
            checks["deep_content_fits"]=Deep.Scroll.ExtentHeight<=Deep.Scroll.ViewportHeight+0.5;
            UI.SaveImage(Main,""+Path.Combine(smokeFolder,"control.png"));
            UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt.png"));
            UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek.png"));
            if(offline) {
                checks["openai_content_fits"]=OpenAi.Scroll.ExtentHeight<=OpenAi.Scroll.ViewportHeight+0.5;
                UI.SaveImage(OpenAi,Path.Combine(smokeFolder,"openai.png"));
                OpenAi.Render(OpenAiData,"网络连接失败，稍后自动重试",false);OpenAi.UpdateLayout();UI.SaveImage(OpenAi,Path.Combine(smokeFolder,"openai-stale.png"));
                OpenAi.Render(null,"API Key 已登录；请补充 Admin API Key 和 API Key ID。",false);OpenAi.UpdateLayout();UI.SaveImage(OpenAi,Path.Combine(smokeFolder,"openai-needs-admin.png"));
                OpenAi.Render(OpenAiData,null,false);
            }
            if(GptData!=null) {
                Gpt.Render(GptData,"网络连接失败，稍后自动重试",false);Gpt.UpdateLayout();UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-stale.png"));
            }
            if(offline) {
                checks["updates_disabled_in_offline_tests"]=updateTimer==null;
                Main.RenderUpdate(null,"当前已是最新版本 v4.1.0");
                checks["updates_latest_state"]=Main.UpdateStatus.Text.Contains("最新版本")&&Main.DownloadUpdate.Content.ToString()=="发布页 ↗";
                var demo=UpdateClient.Parse(UpdateTests.Release("v4.1.0"));
                Main.RenderUpdate(demo,null);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                checks["updates_available_state"]=Main.UpdateStatus.Text.Contains("v4.1.0")&&Main.DownloadUpdate.Content.ToString()=="下载更新 ↗";
                checks["updates_available_fits"]=Main.Scroll.ExtentHeight<=Main.Scroll.ViewportHeight+0.5;
                UI.SaveImage(Main,Path.Combine(smokeFolder,"update-available.png"));
                Main.RenderUpdate(demo,"无法检查更新，可稍后重试或打开发布页。");
                checks["updates_failure_preserves_notice"]=Main.UpdateStatus.Text.Contains("v4.1.0")&&Main.DownloadUpdate.Content.ToString()=="下载更新 ↗";
                Main.RenderUpdate(null,"检查更新超时，可稍后重试或打开发布页。");
                checks["updates_failure_is_not_latest"]=!Main.UpdateStatus.Text.Contains("最新版本");
                Main.RenderUpdate(null,"启动时及每 6 小时自动检查更新");
            }
            if(offline) {
                checks["main_has_custom_chrome"]=Main.WindowStyle==WindowStyle.None&&Main.ResizeMode==ResizeMode.CanMinimize&&!Main.Topmost&&Main.ShowInTaskbar;
                SetOrbMode(true);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                checks["orb_replaces_details"]=Orb.IsVisible&&!Gpt.IsVisible&&Prefs.Gpt;
                checks["orb_shows_remaining"]=Orb.ShortText.Text=="72%"&&Orb.LongText.Text=="39%";
                foreach(int scale in OrbView.Scales){Prefs.OrbScale=scale;Orb.ApplyScale();Orb.UpdateLayout();checks["orb_size_"+scale]=Math.Abs(Orb.Width-76.0*scale/100)<1.1;UI.SaveImage(Orb,Path.Combine(smokeFolder,"orb-"+scale+".png"));}
                var hover=SampleGpt();
                hover.Groups[0].Windows[0].Reset=new DateTimeOffset(2026,12,31,16,5,59,TimeSpan.Zero);
                hover.Groups[0].Windows[1].Reset=new DateTimeOffset(2027,1,6,7,9,0,TimeSpan.FromHours(-5));
                Orb.Render(hover,null,false);
                var hoverText=System.Windows.Automation.AutomationProperties.GetName(Orb);
                checks["orb_tooltip_both_reset_times_beijing"]=Orb.HoverShort.Text=="2027.01.01 00:05  (5h)"&&Orb.HoverWeek.Text=="2027.01.06 20:09  (week)";
                checks["orb_tooltip_layout_and_accessibility"]=Orb.ToolTip is System.Windows.Controls.ToolTip&&Orb.HoverHint.Text=="双击详情 · 右键菜单"&&Orb.HoverHint.FontSize<Orb.HoverShort.FontSize&&hoverText.Contains(Orb.HoverShort.Text)&&hoverText.Contains(Orb.HoverWeek.Text);
                hover.Groups[0].Windows[1].Reset=null;Orb.Render(hover,"模拟断网",false);
                checks["orb_tooltip_missing_reset_and_stale"]=Orb.HoverWeek.Text=="待同步  (week)"&&Orb.HoverHint.Text.Contains("旧数据")&&Orb.HoverShort.Text.Contains("2027.01.01 00:05");
                Orb.Render(null,"模拟首次失败",false);checks["orb_error_without_old_data"]=!Orb.HoverHint.Text.Contains("旧数据")&&Orb.HoverShort.Text.Contains("待同步");
                Orb.Render(null,null,false);checks["orb_unknown_not_zero"]=Orb.ShortText.Text.Contains("—")&&Orb.LongText.Text.Contains("—");
                var boundary=SampleGpt();boundary.Groups[0].Windows[0].Used=100;boundary.Groups[0].Windows[1].Used=0;
                Orb.Render(boundary,null,false);checks["orb_zero_and_full"]=Orb.ShortText.Text.Contains("0%")&&Orb.LongText.Text.Contains("100%");
                boundary.Groups[0].Id="other";Orb.Render(boundary,null,false);checks["orb_does_not_substitute_other_pool"]=Orb.ShortText.Text.Contains("—");
                Prefs.OrbMode=true;Prefs.OrbScale=150;Prefs.Save();var restored=Preferences.Load();checks["orb_preferences_roundtrip"]=restored.OrbMode&&restored.OrbScale==150;
                Prefs.OrbScale=17;Prefs.Save();checks["orb_invalid_scale_falls_back"]=Preferences.Load().OrbScale==100;
                Orb.Render(GptData,"模拟断网",false);checks["orb_marks_stale"]=Orb.HoverHint.Text.Contains("旧数据");UI.SaveImage(Orb,Path.Combine(smokeFolder,"orb-stale.png"));
                Main.GptSwitch.IsChecked=false;checks["switch_hides_both_codex_views"]=!Orb.IsVisible&&!Gpt.IsVisible;
                Main.GptSwitch.IsChecked=true;checks["switch_restores_orb_mode"]=Orb.IsVisible&&!Gpt.IsVisible;
                Prefs.OrbScale=100;Orb.ApplyScale();SetOrbMode(false);checks["details_restore"]=Gpt.IsVisible&&!Orb.IsVisible;
                var mark=Brand.Mark(256);mark.Measure(new Size(256,256));mark.Arrange(new Rect(0,0,256,256));UI.SaveImage(mark,Path.Combine(smokeFolder,"brand.png"));
            }
            if(offline)OpenAiSecretStore.Save(new OpenAiConnection {ApiKey="sk-test-only-ui-openai-key",AdminKey="sk-admin-test-only-ui-openai-key",ApiKeyId="key_ui_test"});
            Main.OpenSettings("OpenAI API");var settings=Main.SettingsPage;Main.UpdateLayout();settings.UpdateLayout();UI.SaveImage(settings,Path.Combine(smokeFolder,"settings.png"));
            if(offline) {
                OpenAiUiTests.Run(settings,checks);SettingsUiTests.Run(this,checks);
                settings.Scroll.ScrollToEnd();settings.UpdateLayout();UI.SaveImage(settings,Path.Combine(smokeFolder,"settings-bottom.png"));
            }
            Main.ShowOverview();
            if(offline) {
                Gpt.Render(GptData,null,false);Main.FitToContent();Gpt.FitToContent();Deep.FitToContent();
                foreach(string theme in new[]{"light","dark","system"}) {
                    Appearance.Apply(theme);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                    Main.UpdateLayout();Gpt.UpdateLayout();Deep.UpdateLayout();
                    UI.SaveImage(Main,Path.Combine(smokeFolder,"control-"+theme+".png"));
                    UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-"+theme+".png"));
                    UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek-"+theme+".png"));
                    OpenAi.UpdateLayout();UI.SaveImage(OpenAi,Path.Combine(smokeFolder,"openai-"+theme+".png"));
                    Prefs.Appearance=theme;Main.OpenSettings("外观设置");Main.UpdateLayout();UI.SaveImage(Main,Path.Combine(smokeFolder,"settings-"+theme+".png"));
                    checks[theme+"_custom_chrome"]=Main.WindowStyle==WindowStyle.None&&Main.Surface.CornerRadius.TopLeft==14;
                    foreach(string page in new[]{"DeepSeek API","OpenAI API","数据目录","关于软件"}) {Main.SettingsPage.SelectPage(page);Main.UpdateLayout();UI.SaveImage(Main,Path.Combine(smokeFolder,"settings-"+theme+"-"+page+".png"));}
                    Main.ShowOverview();
                    SetOrbMode(true);Orb.ContextMenu.PlacementTarget=Orb;Orb.ContextMenu.IsOpen=true;await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                    var menu=Orb.ContextMenu;menu.UpdateLayout();UI.SaveImage(menu,Path.Combine(smokeFolder,"orb-menu-"+theme+".png"));
                    checks[theme+"_menu_background_matches"]=((System.Windows.Media.SolidColorBrush)menu.Background).Color==((System.Windows.Media.SolidColorBrush)Resources["CardBrush"]).Color;
                    checks[theme+"_menu_text_matches"]=((System.Windows.Media.SolidColorBrush)((System.Windows.Controls.MenuItem)menu.Items[0]).Foreground).Color==((System.Windows.Media.SolidColorBrush)Resources["InkBrush"]).Color;
                    menu.IsOpen=false;SetOrbMode(false);
                    checks[theme+"_ink_updates"]=((System.Windows.Media.SolidColorBrush)Main.GptInfo.Foreground).Color==((System.Windows.Media.SolidColorBrush)Resources["MutedBrush"]).Color;
                }
                Appearance.Apply("light");
                foreach(double width in new[]{300.0,368.0,660.0,940.0}) {
                    Gpt.Width=width;Deep.Width=width;OpenAi.Width=width;Gpt.FitToContent();Deep.FitToContent();OpenAi.FitToContent();
                    await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);Gpt.UpdateLayout();Deep.UpdateLayout();
                    UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-"+width+".png"));UI.SaveImage(Deep,Path.Combine(smokeFolder,"deepseek-"+width+".png"));
                    checks["gpt_fits_"+width]=Gpt.Scroll.ExtentHeight<=Gpt.Scroll.ViewportHeight+1;
                    checks["deep_fits_"+width]=Deep.Scroll.ExtentHeight<=Deep.Scroll.ViewportHeight+1;
                    checks["gpt_bottom_gap_"+width]=Math.Abs(Gpt.Scroll.ViewportHeight-Gpt.Scroll.ExtentHeight)<2;
                    OpenAi.UpdateLayout();UI.SaveImage(OpenAi,Path.Combine(smokeFolder,"openai-"+width+".png"));
                    checks["openai_fits_"+width]=OpenAi.Scroll.ExtentHeight<=OpenAi.Scroll.ViewportHeight+1;
                    checks["openai_bottom_gap_"+width]=Math.Abs(OpenAi.Scroll.ViewportHeight-OpenAi.Scroll.ExtentHeight)<2;
                }
                Gpt.Width=300;Gpt.ResizeBy(0,1,0,-1000);await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);Gpt.UpdateLayout();
                checks["minimum_shows_full_content"]=Gpt.Scroll.ExtentHeight<=Gpt.Scroll.ViewportHeight+1;
                checks["resize_keeps_minimum"]=Gpt.Height==Gpt.MinHeight&&Gpt.AutoHeight;
                UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-short.png"));Gpt.FitToContent();
                checks["fit_restores_auto_height"]=Gpt.AutoHeight;
                var original=GptData.ResetCredits;GptData.ResetCredits=0;UpdateLabels();checks["reset_disabled_without_credit"]=!Main.ResetQuota.IsEnabled;
                GptData.ResetCredits=null;UpdateLabels();checks["reset_disabled_when_unknown"]=!Main.ResetQuota.IsEnabled;
                GptData.ResetCredits=original;UpdateLabels();checks["reset_enabled_with_credit"]=Main.ResetQuota.IsEnabled;
                gptBusy=true;UpdateLabels();checks["reset_disabled_while_busy"]=!Main.ResetQuota.IsEnabled;gptBusy=false;
                GptError="模拟连接失败";UpdateLabels();checks["reset_disabled_on_stale_data"]=!Main.ResetQuota.IsEnabled;
                Prefs.PendingResetKey=Guid.NewGuid().ToString();UpdateLabels();checks["uncertain_reset_can_retry"]=Main.ResetQuota.IsEnabled;GptError=null;
                Prefs.PendingResetKey=null;Gpt.Render(null,"模拟首次连接失败",false);Gpt.FitToContent();await Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);UI.SaveImage(Gpt,Path.Combine(smokeFolder,"gpt-empty.png"));
                checks["no_live_queries_or_reset"]=true;
            }
            if(offline)await DashboardUiTests.Run(this,checks,smokeFolder);
            File.WriteAllText(Path.Combine(smokeFolder,"smoke.json"),Json.Write(checks));Quit();
        }
        static GptSnapshot SampleGpt() {
            var data=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"planType\":\"plus\",\"primary\":{\"usedPercent\":28,\"windowDurationMins\":300},\"secondary\":{\"usedPercent\":61,\"windowDurationMins\":10080}},\"rateLimitResetCredits\":{\"availableCount\":2}}"));
            data.Groups[0].Windows[0].Reset=DateTimeOffset.UtcNow.AddHours(3);data.Groups[0].Windows[1].Reset=DateTimeOffset.UtcNow.AddDays(4);return data;
        }
        static DeepSnapshot SampleDeep() {return DeepSnapshot.Parse(Json.Read("{\"is_available\":true,\"balance_infos\":[{\"currency\":\"CNY\",\"total_balance\":\"51.7801\",\"granted_balance\":\"1.00\",\"topped_up_balance\":\"50.7801\"},{\"currency\":\"USD\",\"total_balance\":\"0.10\",\"granted_balance\":\"0\",\"topped_up_balance\":\"0.10\"}]}"));}
        static OpenAiSnapshot SampleOpenAi() {
            var now=DateTimeOffset.UtcNow;var china=UI.China(now);
            return new OpenAiSnapshot {ApiKeyId="key_demo",Fetched=now,Start=new DateTimeOffset(china.Year,china.Month,1,0,0,0,TimeSpan.FromHours(8)).ToUniversalTime(),End=now,
                Today=new OpenAiTokenTotals {InputTokens=125400,OutputTokens=24800,CachedInputTokens=68200,Requests=42},
                Month=new OpenAiTokenTotals {InputTokens=4285700,OutputTokens=813240,CachedInputTokens=1928000,Requests=1236}};
        }
    }
    public static class Program {
        const string MutexName="Local\\TokenMonitor.Ricki.v1";
        [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hWnd,int cmd);
        [STAThread]
        public static int Main(string[] args) {
            if(args.Length>0&&args[0]=="--set-data-dir"&&args.Length!=2)return 1;
            if(args.Length==2&&args[0]=="--apply-update")return UpdateInstaller.Run(args[1]);
            if(args.Length>0&&args[0]=="--self-test") return SelfTests.Run(args.Length>1?args[1]:Path.Combine(Paths.Root,"self-test.json"));
            if(args.Length>0&&args[0]=="--diagnose") return Diagnose(args.Length>1?args[1]:Path.Combine(Paths.Data,"diagnose.json")).GetAwaiter().GetResult();
            if(args.Length>1&&args[0]=="--ui-test") {Paths.TestDataDirectory=Path.Combine(Path.GetFullPath(args[1]),"test-profile");new MonitorApp(args[1],true).Run();return 0;}
            bool fresh;using(var mutex=new Mutex(true,MutexName,out fresh)) {
                if(!fresh) {
                    if(args.Length>0&&args[0]=="--set-data-dir")return 2;
                    // Use a named event instead of window titles: it also works when the control panel is hidden.
                    try {using(var signal=EventWaitHandle.OpenExisting("Local\\TokenMonitor.Show.v1"))signal.Set();}catch{}
                    return 0;
                }
                if(args.Length>1&&args[0]=="--set-data-dir") {
                    try {LocalStorage.Initialize();StorageLocation.Relocate(args[1],Preferences.Load());return 0;}
                    catch {return 1;}
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
            result["data_directory"]=Paths.Data;
            using(var gpt=new CodexProvider())using(var deep=new DeepProvider())using(var openAi=new OpenAiProvider()) {
                try {var s=await gpt.Fetch();result["gpt"]=new {ok=true,groups=s.Groups.Count,windows=s.Groups.Sum(x=>x.Windows.Count)};}catch(Exception e){result["gpt"]=new {ok=false,error=e.GetType().Name};}
                try {var s=await deep.Fetch(Preferences.Load().DeepSource);result["deepseek"]=new {ok=true,currencies=s.Balances.Select(x=>x.Currency).ToArray()};}catch(Exception e){result["deepseek"]=new {ok=false,error=e.GetType().Name};}
                if(File.Exists(Paths.OpenAiKey)) {
                    try {var s=await openAi.Fetch(OpenAiSecretStore.Read());result["openai"]=new {ok=true,today_tokens=s.Today.TotalTokens,month_tokens=s.Month.TotalTokens};}catch(Exception e){result["openai"]=new {ok=false,error=e.GetType().Name};}
                } else result["openai"]=new {configured=false};
            }
            File.WriteAllText(report,Json.Write(result));return 0;
        }
    }
}
