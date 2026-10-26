using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TokenMonitor {
    public static class UI {
        public const string Green="#10A37F", Blue="#4D6BFE", Muted="#7B838D", Ink="#202526";
        public static SolidColorBrush Brush(string hex) { return (SolidColorBrush)new BrushConverter().ConvertFromString(hex); }
        public static TextBlock Text(string s,double size=13,string color=Ink,bool bold=false) {
            var text=new TextBlock { Text=s,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap };
            FontSizing.Bind(text,TextBlock.FontSizeProperty,size);Appearance.Paint(text,TextBlock.ForegroundProperty,color);return text;
        }
        public static Button Button(string s,Action action,string tip=null) {
            var b=new Button { Content=s,ToolTip=tip };b.Click+=(o,e)=>action();return b;
        }
        public static Border Card(UIElement child,int padding=16) {
            var card=new Border { CornerRadius=new CornerRadius(12),Padding=new Thickness(padding),Child=child,Margin=new Thickness(0,0,0,10) };
            Appearance.Paint(card,Border.BackgroundProperty,"#FFFFFF");return card;
        }
        public static Grid Row(UIElement left,UIElement right) {
            var g=new Grid();g.ColumnDefinitions.Add(new ColumnDefinition());g.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            Grid.SetColumn(right,1);g.Children.Add(left);g.Children.Add(right);return g;
        }
        public static void Link(string url) {
            try { Process.Start(new ProcessStartInfo(url){UseShellExecute=true}); }
            catch { MessageBox.Show("无法打开浏览器，请检查默认浏览器设置。","Token Monitor"); }
        }
        public static string Clock(DateTimeOffset time) { return China(time).ToString("HH:mm:ss",CultureInfo.InvariantCulture); }
        public static DateTimeOffset China(DateTimeOffset time) { return time.ToOffset(TimeSpan.FromHours(8)); }
        public static void SaveImage(FrameworkElement view,string path) {
            view.UpdateLayout();
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(view.ActualWidth),(int)Math.Ceiling(view.ActualHeight),96,96,PixelFormats.Pbgra32);
            bitmap.Render(view);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));
            using(var stream=File.Create(path)) png.Save(stream);
        }
    }
    public class Shell : Window {
        public readonly StackPanel Body=new StackPanel();
        public readonly TextBlock Subtitle;
        public readonly Border Surface;
        public readonly ScrollViewer Scroll;
        protected readonly DockPanel ContentPanel;
        protected readonly StackPanel TitleButtons;
        public Action CloseAction;
        public bool Exiting;
        public bool AutoHeight=true;
        public event Action GeometryChanged;
        bool fitQueued,resizing;
        readonly Border titleArea;
        readonly bool formal;
        public Shell(string title,string sub,string accent,double width,double height,bool floating,bool formal=false) {
            this.formal=formal;
            Title=title=="Token Monitor"?title:title+" · Token Monitor";Width=width;Height=height;Topmost=floating;ShowInTaskbar=!floating;
            WindowStyle=formal?WindowStyle.SingleBorderWindow:WindowStyle.None;ResizeMode=formal||floating?ResizeMode.CanResize:ResizeMode.NoResize;AllowsTransparency=!formal;Background=Brushes.Transparent;
            MinWidth=floating?300:width;MinHeight=floating?210:100;
            FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI");
            UseLayoutRounding=true;SnapsToDevicePixels=true;
            var iconPath=System.IO.Path.Combine(Paths.Root,"TokenMonitor.ico");
            if(File.Exists(iconPath)) Icon=BitmapFrame.Create(new Uri(iconPath));
            Surface=new Border { BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(18),Margin=new Thickness(12),
                Effect=new DropShadowEffect { BlurRadius=18,ShadowDepth=3,Opacity=0.12,Color=Colors.Black } };
            Appearance.Paint(Surface,Border.BackgroundProperty,"#F5F7F8");Appearance.Paint(Surface,Border.BorderBrushProperty,"#E2E6E8");
            if(formal) {Surface.Margin=new Thickness(0);Surface.CornerRadius=new CornerRadius(0);Surface.Effect=null;MinWidth=760;MinHeight=560;AutoHeight=false;}
            var root=new Grid();root.Children.Add(Surface);Content=root;
            var layout=new DockPanel { LastChildFill=true };ContentPanel=layout;Surface.Child=layout;
            SourceInitialized+=(s,e)=>Appearance.ApplyTitleBar(this);
            titleArea=new Border { Padding=new Thickness(19,17,14,14),Background=Brushes.Transparent,Cursor=Cursors.SizeAll };
            titleArea.MouseLeftButtonDown+=(s,e)=> { if(e.ChangedButton==MouseButton.Left && !e.Handled) { try {DragMove();} catch {} } };
            var mark=new Border { Background=UI.Brush(accent),Width=7,Height=7,CornerRadius=new CornerRadius(4),Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Center };
            var heading=new StackPanel {Orientation=Orientation.Horizontal};heading.Children.Add(mark);heading.Children.Add(UI.Text(title,14,UI.Ink,true));
            var words=new StackPanel();words.Children.Add(heading);Subtitle=UI.Text(sub,10.5,UI.Muted);Subtitle.Margin=new Thickness(15,4,0,0);words.Children.Add(Subtitle);
            var close=UI.Button("×",()=>{if(CloseAction!=null)CloseAction();else Close();},floating?"隐藏此悬浮窗":"收起到托盘");
            close.FontSize=20;close.Padding=new Thickness(8,0,8,2);close.Background=Brushes.Transparent;close.VerticalAlignment=VerticalAlignment.Top;close.Cursor=Cursors.Hand;
            AutomationProperties.SetName(close,floating?"隐藏"+title:"收起控制面板");
            var titleButtons=new StackPanel { Orientation=Orientation.Horizontal };TitleButtons=titleButtons;
            if(floating) {
                var fit=UI.Button("↕",()=>FitToContent(),"贴合内容高度");fit.Padding=new Thickness(7,3,7,3);fit.FontSize=16;fit.Background=Brushes.Transparent;
                AutomationProperties.SetName(fit,"贴合内容高度");titleButtons.Children.Add(fit);
            }
            if(!formal)titleButtons.Children.Add(close);
            titleArea.Child=UI.Row(words,titleButtons);DockPanel.SetDock(titleArea,Dock.Top);layout.Children.Add(titleArea);
            Scroll=new ScrollViewer { VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(18,0,18,16),Content=Body };
            layout.Children.Add(Scroll);
            Body.SizeChanged+=(s,e)=>QueueFit();
            SizeChanged+=(s,e)=>{if(e.WidthChanged)QueueFit();if(GeometryChanged!=null)GeometryChanged();};
            ContentRendered+=(s,e)=>QueueFit();
            if(floating) {
                AddResizeHandle(root,-1,0);AddResizeHandle(root,1,0);AddResizeHandle(root,0,-1);AddResizeHandle(root,0,1);
                AddResizeHandle(root,-1,-1);AddResizeHandle(root,1,-1);AddResizeHandle(root,-1,1);AddResizeHandle(root,1,1);
            }
            Closing+=(s,e)=>{ if(!Exiting) {e.Cancel=true;if(CloseAction!=null)CloseAction();else Hide();} };
        }
        public void EnableCardStretch(CardPanel cards) {
            EventHandler update=(sender,args)=>{
                if(!cards.IsLoaded)return;
                double content=Body.Children.Cast<FrameworkElement>().Sum(child=>child.DesiredSize.Height);
                double extra=Scroll.ViewportHeight-content;
                double target=Math.Max(0,cards.ActualHeight+extra);
                if(AutoHeight&&!formal)target=0;
                if(Math.Abs(cards.MinHeight-target)>1)cards.MinHeight=target;
            };
            cards.LayoutUpdated+=update;
        }
        void AddResizeHandle(Grid root,int x,int y) {
            var thumb=new Thumb { Width=x==0?double.NaN:10,Height=y==0?double.NaN:10,
                HorizontalAlignment=x<0?HorizontalAlignment.Left:x>0?HorizontalAlignment.Right:HorizontalAlignment.Stretch,
                VerticalAlignment=y<0?VerticalAlignment.Top:y>0?VerticalAlignment.Bottom:VerticalAlignment.Stretch,
                Margin=new Thickness(x==0?24:8,y==0?24:8,x==0?24:8,y==0?24:8),
                Cursor=x==0?Cursors.SizeNS:y==0?Cursors.SizeWE:x==y?Cursors.SizeNWSE:Cursors.SizeNESW,
                Focusable=false,ToolTip="拖动调整大小" };
            var template=new ControlTemplate(typeof(Thumb));var factory=new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty,Brushes.Transparent);template.VisualTree=factory;thumb.Template=template;
            thumb.DragStarted+=(s,e)=>{resizing=true;if(y!=0)AutoHeight=false;};
            thumb.DragDelta+=(s,e)=>ResizeBy(x,y,e.HorizontalChange,e.VerticalChange);
            thumb.DragCompleted+=(s,e)=>{resizing=false;QueueFit();if(GeometryChanged!=null)GeometryChanged();};
            root.Children.Add(thumb);
        }
        public void ResizeBy(int x,int y,double dx,double dy) {
            var area=SystemParameters.WorkArea;
            if(x!=0) {
                double previous=Width;Width=Math.Max(MinWidth,Math.Min(Math.Max(MinWidth,area.Width),Width+x*dx));
                if(x<0)Left+=previous-Width;
            }
            if(y!=0) {
                AutoHeight=false;double previous=Height;Height=Math.Max(MinHeight,Math.Min(Math.Max(MinHeight,area.Height),Height+y*dy));
                if(y<0)Top+=previous-Height;
            }
        }
        public void RestoreSize(double width,double height,bool autoHeight) {
            Width=Math.Min(width,Math.Max(MinWidth,SystemParameters.WorkArea.Width));
            Height=Math.Min(height,Math.Max(MinHeight,SystemParameters.WorkArea.Height));AutoHeight=autoHeight;QueueFit();
        }
        public void FitToContent() {AutoHeight=true;FitHeight();QueueFit();if(GeometryChanged!=null)GeometryChanged();}
        void QueueFit() {
            if(fitQueued||resizing)return;fitQueued=true;
            Dispatcher.BeginInvoke(new Action(()=>{fitQueued=false;if(!resizing)FitHeight();}),DispatcherPriority.Loaded);
        }
        static void ResetCardHeights(DependencyObject parent) {
            var cards=parent as CardPanel;if(cards!=null)cards.MinHeight=0;
            foreach(var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())ResetCardHeights(child);
        }
        public void FitHeight() {
            if(formal)return;
            ResetCardHeights(Body);
            Body.InvalidateMeasure();
            double width=Math.Max(0,Width-26);
            titleArea.Measure(new Size(width,double.PositiveInfinity));
            Body.Measure(new Size(Math.Max(0,width-Scroll.Padding.Left-Scroll.Padding.Right),double.PositiveInfinity));
            double needed=26+titleArea.DesiredSize.Height+Body.DesiredSize.Height+Scroll.Padding.Top+Scroll.Padding.Bottom;
            double height=Math.Max(MinHeight,Math.Min(SystemParameters.WorkArea.Height,needed));
            // Floating utilities never grow into an empty canvas; width still reflows cards.
            MaxHeight=Math.Min(SystemParameters.WorkArea.Height,height);
            if((AutoHeight||Height>MaxHeight)&&Math.Abs(Height-height)>0.5)Height=height;
        }
        public void Place(double x,double y,double fallbackX,double fallbackY) {
            Left=double.IsNaN(x)||double.IsInfinity(x)||x==-1?fallbackX:x;
            Top=double.IsNaN(y)||double.IsInfinity(y)||y==-1?fallbackY:y;
            double minX=SystemParameters.VirtualScreenLeft,minY=SystemParameters.VirtualScreenTop;
            double maxX=minX+SystemParameters.VirtualScreenWidth,maxY=minY+SystemParameters.VirtualScreenHeight;
            if(Left<minX-Width+80||Left>maxX-80||Top<minY||Top>maxY-60) {Left=fallbackX;Top=fallbackY;}
        }
    }
    public sealed class MainView : Shell {
        public readonly CheckBox GptSwitch,DeepSwitch,OpenAiSwitch;
        public readonly TextBlock GptInfo,DeepInfo,OpenAiInfo;
        public readonly ComboBox Interval;
        public readonly Button Refresh;
        public readonly Button CheckUpdate,DownloadUpdate;
        public readonly TextBlock UpdateStatus;
        readonly MonitorApp app;
        readonly Grid workspace=new Grid();
        readonly Dictionary<string,Button> navigation=new Dictionary<string,Button>();
        HistoryView history;
        readonly CardPanel services;
        readonly DashboardCard gptCard,deepCard,openCard;
        public SettingsView SettingsPage {get;private set;}
        public MainView(MonitorApp app):base("Token Monitor","用量控制中心  /  v3.2.0",UI.Green,1180,760,false,true) {
            this.app=app;
            ResizeMode=ResizeMode.CanMinimize;Width=Math.Min(1180,SystemParameters.WorkArea.Width);Height=Math.Min(760,SystemParameters.WorkArea.Height);MinWidth=Math.Min(760,Width);MinHeight=Math.Min(560,Height);
            ContentPanel.Children.Remove(Scroll);
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(170)});layout.ColumnDefinitions.Add(new ColumnDefinition());
            var nav=new StackPanel {Margin=new Thickness(14,4,10,14)};layout.Children.Add(nav);Grid.SetColumn(workspace,1);layout.Children.Add(workspace);workspace.Children.Add(Scroll);ContentPanel.Children.Add(layout);
            foreach(string name in new[]{"账户概览","历史趋势","外观设置","ChatGPT 连接","DeepSeek 密钥","OpenAI 密钥","数据目录","关于软件"}) {string target=name;var button=UI.Button(name,()=>{if(target=="账户概览")ShowOverview();else if(target=="历史趋势")ShowHistory();else OpenSettings(target);});button.Margin=new Thickness(0,0,0,name=="历史趋势"?24:8);navigation.Add(name,button);nav.Children.Add(button);}
            SelectNavigation("账户概览");
            GptInfo=UI.Text("选择连接设置，连接本机账号",11.5,UI.Muted);
            DeepInfo=UI.Text("选择连接设置，添加余额来源",11.5,UI.Muted);
            OpenAiInfo=UI.Text("选择连接设置，接入历史用量",11.5,UI.Muted);
            GptSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Gpt };
            DeepSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Deep };
            OpenAiSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.OpenAi };
            AutomationProperties.SetName(GptSwitch,"显示 GPT 用量悬浮窗");AutomationProperties.SetName(DeepSwitch,"显示 DeepSeek 用量悬浮窗");
            AutomationProperties.SetName(OpenAiSwitch,"显示 OpenAI API 用量悬浮窗");
            var intro=new StackPanel {Margin=new Thickness(0,4,0,18)};
            intro.Children.Add(UI.Text("账户与用量",22,UI.Ink,true));
            intro.Children.Add(UI.Text(app.FirstRun?"先选择要监控的服务，再完成连接。浮窗可按需打开。":"额度、余额与 API 用量 · 北京时间",12,UI.Muted));Body.Children.Add(intro);
            services=new CardPanel {Margin=new Thickness(0,0,0,18)};
            gptCard=new DashboardCard("ChatGPT","Codex / Work",GptInfo,GptSwitch,()=>OpenSettings("ChatGPT 连接"),()=>{app.Main.GptSwitch.IsChecked=true;app.SetOrbMode(true);});
            deepCard=new DashboardCard("DeepSeek","API / Harness",DeepInfo,DeepSwitch,()=>OpenSettings("DeepSeek 密钥"));
            openCard=new DashboardCard("OpenAI","API 用量",OpenAiInfo,OpenAiSwitch,()=>OpenSettings("OpenAI 密钥"));
            services.Children.Add(gptCard);services.Children.Add(deepCard);services.Children.Add(openCard);Body.Children.Add(services);
            services.VerticalAlignment=VerticalAlignment.Top;
            GptSwitch.Checked+=(s,e)=>app.SetGpt(true);GptSwitch.Unchecked+=(s,e)=>app.SetGpt(false);
            DeepSwitch.Checked+=(s,e)=>app.SetDeep(true);DeepSwitch.Unchecked+=(s,e)=>app.SetDeep(false);
            OpenAiSwitch.Checked+=(s,e)=>app.SetOpenAi(true);OpenAiSwitch.Unchecked+=(s,e)=>app.SetOpenAi(false);
            Interval=new ComboBox { Width=85,Height=28,FontSize=12,VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=new[]{"15 秒","30 秒","60 秒","120 秒"} };
            Interval.SelectedIndex=Array.IndexOf(new[]{15,30,60,120},app.Prefs.Interval);
            AutomationProperties.SetName(Interval,"自动刷新间隔");
            Interval.SelectionChanged+=(s,e)=>{app.Prefs.Interval=new[]{15,30,60,120}[Interval.SelectedIndex];app.Save();app.UpdateLabels();};
            var auto=new StackPanel {Orientation=Orientation.Horizontal};var autoLabel=UI.Text("自动刷新",12,UI.Muted);autoLabel.VerticalAlignment=VerticalAlignment.Center;autoLabel.Margin=new Thickness(0,0,12,0);auto.Children.Add(autoLabel);auto.Children.Add(Interval);auto.Margin=new Thickness(2,3,2,16);Body.Children.Add(auto);
            Refresh=UI.Button("立即刷新",async()=>await app.RefreshAll(true));Refresh.SetResourceReference(Button.BackgroundProperty,"ActionBrush");Refresh.SetResourceReference(Button.ForegroundProperty,"ActionInkBrush");
            var actions=new StackPanel { Orientation=Orientation.Horizontal };
            actions.Children.Add(Refresh);var settings=UI.Button("设置",()=>OpenSettings("外观设置"));settings.Margin=new Thickness(8,0,0,0);actions.Children.Add(settings);
            var exit=UI.Button("退出",()=>app.Quit());exit.Margin=new Thickness(8,0,0,0);actions.Children.Add(exit);TitleButtons.Children.Add(actions);
            var note=UI.Text("悬浮球可拖动 · 双击展开详情 · 右键打开菜单 · 关闭主界面后继续在托盘运行",10.5,UI.Muted);note.Margin=new Thickness(1,13,0,0);Body.Children.Add(note);
            var updatePanel=new StackPanel {Margin=new Thickness(0,14,0,0)};
            UpdateStatus=UI.Text("启动时及每 6 小时自动检查更新",11,UI.Muted);updatePanel.Children.Add(UpdateStatus);
            var updateActions=new WrapPanel {Margin=new Thickness(0,8,0,0)};
            CheckUpdate=UI.Button("检查更新",async()=>await app.CheckUpdates(true));updateActions.Children.Add(CheckUpdate);
            DownloadUpdate=UI.Button("发布页 ↗",async()=>await app.InstallUpdate());DownloadUpdate.Margin=new Thickness(8,0,0,0);updateActions.Children.Add(DownloadUpdate);
            updatePanel.Children.Add(updateActions);
            RenderMetrics();CloseAction=()=>Hide();
        }
        void SelectNavigation(string name) {foreach(var pair in navigation)pair.Value.SetResourceReference(Button.BackgroundProperty,pair.Key==name?"ButtonBrush":"SurfaceBrush");}
        public void OpenSettings(string page) {
            if(SettingsPage!=null&&SettingsPage.Busy)return;
            if(SettingsPage==null)SettingsPage=new SettingsView(app,ShowOverview,true);
            workspace.Children.Clear();workspace.Children.Add(SettingsPage);
            SettingsPage.SelectPage(page);SelectNavigation(page);Subtitle.Text="设置  /  v3.2.0";
        }
        public void HistoryChanged() {if(history!=null&&workspace.Children.Contains(history))history.Render();}
        public void ShowHistory() {
            if(SettingsPage!=null&&SettingsPage.Busy)return;
            if(SettingsPage!=null){SettingsPage.ClearSecrets();SettingsPage=null;}
            history=new HistoryView(app);workspace.Children.Clear();workspace.Children.Add(history);SelectNavigation("历史趋势");Subtitle.Text="历史趋势  /  v3.2.0";
        }
        public void ShowOverview() {
            if(SettingsPage!=null){if(SettingsPage.Busy)return;SettingsPage.ClearSecrets();SettingsPage=null;}
            workspace.Children.Clear();workspace.Children.Add(Scroll);SelectNavigation("账户概览");Subtitle.Text="用量控制中心  /  v3.2.0";
        }
        public void RenderUpdate(AvailableRelease release,string message) {
            UpdateStatus.Text=(release==null?"":"发现新版本 "+release.Tag+"，可下载更新。")+(message==null?"":(release==null?"":"\n")+message)+(release==null||string.IsNullOrWhiteSpace(release.Notes)?"":"\n\n"+release.Notes);
            DownloadUpdate.Content=release==null?"发布页 ↗":"下载更新 ↗";
            FitHeight();
        }
        public void RenderMetrics() {gptCard.Render(app.GptData,0);deepCard.Render(app.DeepData,1);openCard.Render(app.OpenAiData,2);}
    }
    public sealed class UsageView : Shell {
        readonly MonitorApp app;
        public readonly bool Gpt;
        readonly StackPanel details=new StackPanel();
        readonly TextBlock status=UI.Text("正在连接…",10.5,UI.Muted);
        readonly TextBlock interval=UI.Text("",10,UI.Muted);
        readonly List<Tuple<TextBlock,QuotaWindow>> clocks=new List<Tuple<TextBlock,QuotaWindow>>();
        public readonly Button Refresh;
        public readonly Button ResetQuota;
        public UsageView(MonitorApp owner,bool gpt):base(gpt?"ChatGPT":"DeepSeek",gpt?"Codex / ChatGPT Work":"API 账户余额",gpt?UI.Green:UI.Blue,368,gpt?500:445,true) {
            app=owner;Gpt=gpt;Body.Children.Add(details);
            var buttons=new WrapPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,-6) };
            Refresh=UI.Button("刷新",async()=>{if(Gpt)await app.RefreshGpt(true);else await app.RefreshDeep(true);});Refresh.Margin=new Thickness(0,0,6,6);buttons.Children.Add(Refresh);
            if(Gpt) {var compact=UI.Button("收为悬浮球",()=>app.SetOrbMode(true));compact.Margin=new Thickness(0,0,6,6);buttons.Children.Add(compact);ResetQuota=UI.Button("重置额度",async()=>await app.ResetGptQuota(),"使用账户的官方额度重置次数");ResetQuota.Margin=new Thickness(0,0,6,6);ResetQuota.IsEnabled=false;buttons.Children.Add(ResetQuota);}
            var official=UI.Button("官方用量 ↗",()=>UI.Link(Gpt?"https://chatgpt.com/codex/settings/usage":"https://platform.deepseek.com/usage"));official.Margin=new Thickness(0,0,6,6);buttons.Children.Add(official);
            var controls=UI.Button("⋯",()=>app.ShowControl(),"打开控制面板");controls.FontSize=16;controls.Padding=new Thickness(12,3,12,3);
            controls.Margin=new Thickness(0,0,0,6);buttons.Children.Add(controls);Body.Children.Add(buttons);
            status.Margin=new Thickness(1,13,0,0);Body.Children.Add(status);
            interval.Margin=new Thickness(1,5,0,0);Body.Children.Add(interval);
            CloseAction=()=>{if(Gpt)app.Main.GptSwitch.IsChecked=false;else app.Main.DeepSwitch.IsChecked=false;};
            Render(null,null,false);
        }
        public void Render(object data,string error,bool loading) {
            details.Children.Clear();clocks.Clear();
            if(Gpt && data is GptSnapshot) RenderGpt((GptSnapshot)data);
            else if(!Gpt && data is DeepSnapshot) RenderDeep((DeepSnapshot)data);
            else {
                var empty=new StackPanel { Margin=new Thickness(0,12,0,15) };
                empty.Children.Add(UI.Text(loading?"正在同步":"暂时无法获取",20,UI.Ink,true));
                var desc=UI.Text(loading?"连接官方服务，读取你的账户用量。":error??"等待下一次刷新。",12,UI.Muted);desc.Margin=new Thickness(0,14,0,0);empty.Children.Add(desc);details.Children.Add(UI.Card(empty));
            }
            details.Opacity=error!=null&&data!=null?0.58:1;
            Refresh.IsEnabled=!loading;
            UpdateStatus(data,error,loading);
        }
        void RenderGpt(GptSnapshot snapshot) {
            var plan=snapshot.Groups.FirstOrDefault(x=>x.Id=="codex")??snapshot.Groups[0];
            Subtitle.Text="Codex / ChatGPT Work"+(string.IsNullOrEmpty(plan.Plan)?"":"  ·  "+CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plan.Plan));
            foreach(var group in snapshot.Groups) {
                if(snapshot.Groups.Count>1) { var title=UI.Text(group.Name,11.5,UI.Muted,true);title.Margin=new Thickness(2,0,0,8);details.Children.Add(title); }
                if(group.Windows.Count==0) details.Children.Add(UI.Card(UI.Text("官方未返回此额度的时间窗口",12,UI.Muted)));
                var cards=new CardPanel {Margin=new Thickness(0,0,0,10)};
                foreach(var w in group.Windows) {
                    var box=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
                    var pct=new StackPanel {Orientation=Orientation.Horizontal};pct.Children.Add(UI.Text(w.Remaining.HasValue?w.Remaining.Value.ToString("0.#")+"%":"—",25,UI.Ink,true));
                    var remaining=UI.Text(" 剩余",10.5,UI.Muted);remaining.VerticalAlignment=VerticalAlignment.Bottom;remaining.Margin=new Thickness(0,0,0,4);pct.Children.Add(remaining);
                    var label=UI.Text(w.Label,12.5,UI.Ink,true);label.VerticalAlignment=VerticalAlignment.Center;box.Children.Add(UI.Row(label,pct));
                    var track=new Grid {Height=5,Margin=new Thickness(0,12,0,10)};
                    var trackBg=new Border {CornerRadius=new CornerRadius(3)};Appearance.Paint(trackBg,Border.BackgroundProperty,"#E9EEEB");track.Children.Add(trackBg);
                    if(w.Remaining.HasValue) {
                        double remain=Math.Max(0,Math.Min(100,w.Remaining.Value));
                        var bar=new Border { CornerRadius=new CornerRadius(3),HorizontalAlignment=HorizontalAlignment.Left };Appearance.Paint(bar,Border.BackgroundProperty,remain<=10?"#DD6A42":UI.Green);
                        track.SizeChanged+=(s,e)=>bar.Width=track.ActualWidth*remain/100;track.Children.Add(bar);
                    }
                    box.Children.Add(track);var reset=UI.Text("",10.5,UI.Muted);box.Children.Add(reset);clocks.Add(Tuple.Create(reset,w));var card=UI.Card(box,12);card.Margin=new Thickness(0);cards.Children.Add(card);
                }
                if(cards.Children.Count>0)details.Children.Add(cards);
                if(group.Credits!=null) {var credits=UI.Row(UI.Text("额外 credits",11.5,UI.Muted),UI.Text(group.Credits,11.5,UI.Ink,true));credits.Margin=new Thickness(2,1,2,10);details.Children.Add(credits);}
            }
            var resetCount=UI.Text(snapshot.ResetCredits.HasValue?"可用额度重置："+snapshot.ResetCredits.Value+" 次":"官方未返回额度重置次数",10.5,UI.Muted);resetCount.Margin=new Thickness(2,0,0,12);details.Children.Add(resetCount);
            UpdateClocks();
        }
        void RenderDeep(DeepSnapshot snapshot) {
            var cards=new CardPanel {Margin=new Thickness(0,0,0,10)};
            foreach(var b in snapshot.Balances) {
                var box=new StackPanel {VerticalAlignment=VerticalAlignment.Center};box.Children.Add(UI.Row(UI.Text("可用余额",12,UI.Muted),UI.Text(b.Currency,10.5,UI.Blue,true)));
                var big=UI.Text(b.Format(b.Total),36,UI.Ink,true);big.Margin=new Thickness(0,10,0,12);box.Children.Add(big);
                var divider=new Border { Height=1,Margin=new Thickness(0,0,0,14) };Appearance.Paint(divider,Border.BackgroundProperty,"#EEF0F3");box.Children.Add(divider);
                box.Children.Add(UI.Row(UI.Text("充值余额",12,UI.Muted),UI.Text(b.Format(b.ToppedUp),13,UI.Ink,true)));
                var gift=UI.Row(UI.Text("赠送余额",12,UI.Muted),UI.Text(b.Format(b.Granted),13,UI.Ink,true));gift.Margin=new Thickness(0,9,0,0);box.Children.Add(gift);var card=UI.Card(box,12);card.Margin=new Thickness(0);cards.Children.Add(card);
            }
            details.Children.Add(cards);
            var available=UI.Text(snapshot.FromHarness?"来源：DeepSeek Harness · 充值 + 赠送钱包":snapshot.Available?"●  余额充足，可调用 API":"●  余额不足，暂不可调用 API",11,(snapshot.FromHarness||snapshot.Available)?UI.Blue:"#CA6547");available.Margin=new Thickness(2,1,0,9);details.Children.Add(available);
            var note=UI.Text("Token 用量与消费明细见官方用量页",10.5,UI.Muted);note.Margin=new Thickness(2,0,0,14);details.Children.Add(note);
        }
        public void UpdateStatus(object data,string error,bool loading) {
            DateTimeOffset? fetched=data is GptSnapshot?(DateTimeOffset?)((GptSnapshot)data).Fetched:data is DeepSnapshot?(DateTimeOffset?)((DeepSnapshot)data).Fetched:null;
            Appearance.Paint(status,TextBlock.ForegroundProperty,error!=null?"#B77436":UI.Muted);
            status.Text=error!=null?(fetched.HasValue?"旧数据 · ":"")+error:loading?"正在刷新…":fetched.HasValue?"已同步 "+UI.Clock(fetched.Value):"等待同步";
            interval.Text=error!=null&&fetched.HasValue?"上次成功 "+UI.China(fetched.Value).ToString("MM-dd HH:mm:ss")+" · 北京时间":"每 "+app.Prefs.Interval+" 秒同步 · 北京时间";
            Refresh.IsEnabled=!loading;
            if(ResetQuota!=null) {
                var snapshot=data as GptSnapshot;bool retry=!string.IsNullOrEmpty(app.Prefs.PendingResetKey);
                ResetQuota.Content=retry?"确认上次重置":snapshot!=null&&snapshot.ResetCredits.HasValue?"重置额度 ("+snapshot.ResetCredits.Value+")":"重置额度";
                ResetQuota.IsEnabled=!loading&&(retry||(error==null&&snapshot!=null&&snapshot.ResetCredits.GetValueOrDefault()>0));
            }
            UpdateClocks();
        }
        public void UpdateClocks() {
            foreach(var pair in clocks) {
                var reset=pair.Item2.Reset;
                if(!reset.HasValue) {pair.Item1.Text="官方未返回重置时间";continue;}
                var left=reset.Value-DateTimeOffset.UtcNow;
                string countdown=left.TotalSeconds<=0?"等待官方刷新":left.TotalDays>=1?((int)left.TotalDays)+"天 "+left.Hours+"小时后":left.TotalHours>=1?((int)left.TotalHours)+"小时 "+left.Minutes+"分钟后":Math.Max(1,(int)Math.Ceiling(left.TotalMinutes))+"分钟后";
                pair.Item1.Text="重置 "+UI.China(reset.Value).ToString("MM-dd HH:mm")+" · "+countdown;
            }
        }
    }
    public sealed class SettingsView : UserControl {
        readonly Dictionary<string,StackPanel> pages=new Dictionary<string,StackPanel>();
        readonly Dictionary<string,Button> tabs=new Dictionary<string,Button>();
        readonly TextBlock heading=UI.Text("",24,UI.Ink,true);
        readonly TextBlock pageHint=UI.Text("切换分类会保留未保存的输入；返回概览会放弃未保存的修改。",11,UI.Muted);
        readonly ScrollViewer scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        Button saveButton;
        TextBlock feedback;
        string openAiReadIssue;
        public bool Busy {get;private set;}
        public string SelectedPage {get;private set;}
        public ScrollViewer Scroll {get{return scroll;}}
        public SettingsView(MonitorApp app,Action back,bool embedded=false) {
            var root=new Grid {Margin=new Thickness(18,4,18,20)};root.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(170)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=root;
            var nav=new StackPanel {Margin=new Thickness(0,0,20,0)};root.Children.Add(nav);
            var returnButton=UI.Button("← 返回概览",()=>{if(!Busy)back();});returnButton.Margin=new Thickness(0,0,0,28);nav.Children.Add(returnButton);
            nav.Children.Add(UI.Text("设置",12,UI.Muted,true));
            var pageGrid=new Grid();scroll.Content=pageGrid;
            foreach(string name in new[]{"外观设置","ChatGPT 连接","DeepSeek 密钥","OpenAI 密钥","数据目录","关于软件"}) {
                var panel=new StackPanel();pages.Add(name,panel);pageGrid.Children.Add(panel);
                string target=name;var tab=UI.Button(name,()=>SelectPage(target));tab.Margin=new Thickness(0,6,0,0);tabs.Add(name,tab);nav.Children.Add(tab);
            }
            if(embedded){nav.Visibility=Visibility.Collapsed;root.ColumnDefinitions[0].Width=new GridLength(0);}
            var right=new DockPanel {LastChildFill=true,Margin=new Thickness(embedded?0:16,4,4,0)};Grid.SetColumn(right,1);root.Children.Add(right);
            var header=new StackPanel {Margin=new Thickness(0,0,0,22)};header.Children.Add(heading);pageHint.Margin=new Thickness(0,8,0,0);header.Children.Add(pageHint);DockPanel.SetDock(header,Dock.Top);right.Children.Add(header);
            var footer=new StackPanel {Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(footer,Dock.Bottom);right.Children.Add(footer);right.Children.Add(scroll);
            StackPanel Body=pages["外观设置"];

            var appearance=new ComboBox {Height=32,FontSize=12,ItemsSource=new[]{"随系统","浅色","深色"},SelectedIndex=Array.IndexOf(new[]{"system","light","dark"},app.Prefs.Appearance)};
            AutomationProperties.SetName(appearance,"外观主题");
            var appearanceRow=UI.Row(UI.Text("外观",13,UI.Ink,true),appearance);appearance.Width=140;appearanceRow.Margin=new Thickness(2,0,2,16);Body.Children.Add(appearanceRow);
            var fontSize=new ComboBox {Width=160,Height=34,ItemsSource=new[]{"小","中（默认）","大"},SelectedIndex=Array.IndexOf(new[]{"small","medium","large"},app.Prefs.FontSize)};
            AutomationProperties.SetName(fontSize,"界面字号");var fontRow=UI.Row(UI.Text("界面字号",13,UI.Ink,true),fontSize);fontRow.Margin=new Thickness(2,0,2,16);Body.Children.Add(fontRow);
            fontSize.SelectionChanged+=(s,e)=>{string old=app.Prefs.FontSize;try{app.Prefs.FontSize=new[]{"small","medium","large"}[Math.Max(0,fontSize.SelectedIndex)];app.Prefs.Save();FontSizing.Apply(app.Prefs.FontSize);}catch{app.Prefs.FontSize=old;FontSizing.Apply(old);if(feedback!=null){feedback.Text="字号未保存，请检查数据目录权限。";feedback.Visibility=Visibility.Visible;}}};
            var orbSize=new ComboBox {Width=160,Height=32,ItemsSource=new[]{"80% · 紧凑","100% · 约 2 cm","125% · 清晰","150% · 大号","200% · 特大"},SelectedIndex=Array.IndexOf(OrbView.Scales,app.Prefs.OrbScale)};
            AutomationProperties.SetName(orbSize,"ChatGPT 悬浮球大小");
            var orbRow=UI.Row(UI.Text("悬浮球大小",13,UI.Ink,true),orbSize);orbRow.Margin=new Thickness(2,0,2,12);Body.Children.Add(orbRow);
            var orbHint=UI.Text("100% 为 76 个逻辑像素；实际尺寸取决于显示器和 Windows 缩放。上下数值均为剩余额度。",11,UI.Muted);orbHint.Margin=new Thickness(2,0,2,16);Body.Children.Add(orbHint);
            var refreshInterval=new ComboBox {Width=160,Height=32,ItemsSource=new[]{"15 秒","30 秒","60 秒","120 秒"}};
            refreshInterval.SetBinding(ComboBox.SelectedIndexProperty,new System.Windows.Data.Binding("SelectedIndex"){Source=app.Main.Interval,Mode=System.Windows.Data.BindingMode.TwoWay});
            AutomationProperties.SetName(refreshInterval,"设置自动刷新间隔");Body.Children.Add(UI.Row(UI.Text("自动刷新",13,UI.Ink,true),refreshInterval));
            Body=pages["ChatGPT 连接"];
            Body.Children.Add(UI.Text("使用本机 Codex 账号",18,UI.Ink,true));
            var chatHint=UI.Text("1. 在 Codex 中登录你的 ChatGPT 账号。\n2. 点击下方连接，读取 Codex / ChatGPT Work 共用额度。\n3. 返回概览，按需开启浮窗或悬浮球。",13,UI.Muted);chatHint.Margin=new Thickness(0,16,0,20);Body.Children.Add(chatHint);
            var chatState=UI.Text(app.GptData!=null?"已连接本机账号":"等待连接",12,UI.Muted);Body.Children.Add(chatState);
            var chatConnect=UI.Button("连接本机账号",async()=>{app.Prefs.MonitorGpt=true;app.Save();await app.RefreshGpt(true);chatState.Text=app.GptError??"已连接，返回概览查看额度。";});chatConnect.HorizontalAlignment=HorizontalAlignment.Left;chatConnect.Margin=new Thickness(0,16,0,0);Body.Children.Add(chatConnect);
            Body=pages["DeepSeek 密钥"];
            var source=new ComboBox {Height=32,FontSize=12,ItemsSource=new[]{"手动 API Key","DeepSeek Harness 登录账户"},SelectedIndex=app.Prefs.DeepSource=="harness"?1:0};
            AutomationProperties.SetName(source,"DeepSeek 余额来源");
            Body.Children.Add(UI.Text("DeepSeek 余额来源",13,UI.Ink,true));Body.Children.Add(source);
            var harnessHint=UI.Text("Harness 模式读取本机登录状态，无需填写 API Key。请先在 Harness 登录；凭据不会复制保存。",11,UI.Muted);harnessHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(harnessHint);
            var label=UI.Text("DeepSeek API Key",13,UI.Ink,true);label.Margin=new Thickness(2,3,0,10);Body.Children.Add(label);
            var input=PasswordInput("新的 DeepSeek API Key");Body.Children.Add(input);
            var hint=UI.Text(File.Exists(Paths.Key)?"已保存密钥。留空可保留原密钥。":"尚未设置密钥。",11,UI.Muted);hint.Margin=new Thickness(2,9,0,15);Body.Children.Add(hint);

            Action sourceChanged=()=>{input.IsEnabled=source.SelectedIndex==0;label.Visibility=hint.Visibility=input.Visibility=source.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;};
            source.SelectionChanged+=(s,e)=>sourceChanged();sourceChanged();
            Body=pages["OpenAI 密钥"];
            OpenAiConnection existing=null;string readError=null;
            try {if(File.Exists(Paths.OpenAiKey))existing=OpenAiSecretStore.Read();}
            catch(Exception e) {readError=LocalStorage.Explain(e,"读取 OpenAI 密钥")+" 请重新填写 OpenAI API Key。";}
            var openAiLabel=UI.Text("1  验证 API 密钥",16,UI.Ink,true);openAiLabel.Margin=new Thickness(2,3,0,10);Body.Children.Add(openAiLabel);
            var api=PasswordInput("新的 OpenAI API Key");Body.Children.Add(api);
            var loginHint=UI.Text(existing!=null?"已保存普通密钥。留空可保留。":"填写你调用 API 时使用的 sk- 密钥，点击下方验证。",11,UI.Muted);loginHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(loginHint);
            var adminLabel=UI.Text("2  连接历史用量 · Admin API Key",16,UI.Ink,true);adminLabel.Margin=new Thickness(2,0,0,8);Body.Children.Add(adminLabel);
            var admin=PasswordInput("新的 OpenAI Admin API Key");Body.Children.Add(admin);
            var adminHint=UI.Text(existing!=null&&!string.IsNullOrEmpty(existing.AdminKey)?"已保存管理密钥。留空可保留。":"读取用量需要组织管理员创建的管理密钥。",11,UI.Muted);adminHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(adminHint);
            var idLabel=UI.Text("要统计的密钥标识 · API Key ID",12,UI.Ink,true);idLabel.Margin=new Thickness(2,0,0,8);Body.Children.Add(idLabel);
            var idInput=new TextBox {Text=existing==null?"":existing.ApiKeyId??"",Height=37,Padding=new Thickness(10,7,10,7),FontSize=12,BorderThickness=new Thickness(1)};
            idInput.SetResourceReference(TextBox.BackgroundProperty,"CardBrush");idInput.SetResourceReference(TextBox.ForegroundProperty,"InkBrush");idInput.SetResourceReference(TextBox.BorderBrushProperty,"InputBorderBrush");idInput.SetResourceReference(TextBox.CaretBrushProperty,"InkBrush");
            AutomationProperties.SetName(idInput,"OpenAI API Key ID，key_ 开头");Body.Children.Add(idInput);
            var idHint=UI.Text("这是 key_ 开头的标识，不是 sk- 密钥。请从组织的项目密钥管理中获取对应 ID；不确定时请联系组织管理员。更换普通密钥后需重新填写。",11,UI.Muted);idHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(idHint);
            var openAiInfo=UI.Text("密钥验证成功 ≠ 历史用量已连接。没有管理权限？可以先保存普通密钥，再请组织管理员提供用量读取权限与对应 ID。",11.5,UI.Muted);openAiInfo.Margin=new Thickness(2,0,0,12);Body.Children.Add(openAiInfo);
            var connectionState=UI.Text(app.OpenAiData!=null?"历史用量已连接 · 今日 / 本月数据已同步":existing==null?"尚未验证密钥":OpenAiConnectionHelp.Next(existing),12,UI.Muted);connectionState.Margin=new Thickness(2,0,0,16);Body.Children.Add(connectionState);
            bool suppressReset=false,keyIdReset=false;
            api.PasswordChanged+=(s,e)=>{
                if(!suppressReset&&!keyIdReset&&existing!=null&&api.Password.Length>0&&api.Password.Trim()!=existing.ApiKey) {idInput.Text="";keyIdReset=true;}
            };
            var message=UI.Text(readError??LocalStorage.Notice??"",11,"#B77436");feedback=message;openAiReadIssue=readError;message.Visibility=string.IsNullOrEmpty(message.Text)?Visibility.Collapsed:Visibility.Visible;message.Margin=new Thickness(2,0,0,10);
            Action<string> showMessage=text=>{message.Text=text;message.Visibility=Visibility.Visible;};
            bool applyingAppearance=false;
            Action applyAppearance=()=>{
                if(applyingAppearance)return;
                string previous=app.Prefs.Appearance;int previousScale=app.Prefs.OrbScale;
                try {
                    app.Prefs.Appearance=new[]{"system","light","dark"}[Math.Max(0,appearance.SelectedIndex)];
                    app.Prefs.OrbScale=OrbView.Scales[Math.Max(0,orbSize.SelectedIndex)];app.Prefs.Save();
                    Appearance.Apply(app.Prefs.Appearance);app.Orb.ApplyScale();showMessage("已自动保存，即时生效。");
                } catch(Exception e) {
                    app.Prefs.Appearance=previous;app.Prefs.OrbScale=previousScale;
                    applyingAppearance=true;appearance.SelectedIndex=Array.IndexOf(new[]{"system","light","dark"},previous);orbSize.SelectedIndex=Array.IndexOf(OrbView.Scales,previousScale);applyingAppearance=false;
                    showMessage(LocalStorage.Explain(e,"保存外观设置"));
                }
            };
            appearance.SelectionChanged+=(s,e)=>applyAppearance();orbSize.SelectionChanged+=(s,e)=>applyAppearance();
            Func<OpenAiConnection> compose=()=>{
                string newKey=api.Password.Trim();bool replacing=newKey.Length>0&&(existing==null||newKey!=existing.ApiKey);
                return new OpenAiConnection {
                    ApiKey=newKey.Length>0?newKey:existing==null?null:existing.ApiKey,
                    AdminKey=admin.Password.Trim().Length>0?admin.Password.Trim():existing==null?null:existing.AdminKey,
                    ApiKeyId=idInput.Text.Trim().Length>0?idInput.Text.Trim():replacing?null:existing==null?null:existing.ApiKeyId
                };
            };

            Func<System.Threading.Tasks.Task> connectCurrent=async()=>{
                var connection=compose();await app.ConnectOpenAi(connection);existing=connection;openAiReadIssue=null;
                suppressReset=true;api.Clear();admin.Clear();idInput.Text=existing.ApiKeyId??"";suppressReset=false;keyIdReset=false;
                loginHint.Text="密钥验证成功。留空可保留。";connectionState.Text=app.OpenAiData!=null?"历史用量已连接 · 今日 / 本月数据已同步":OpenAiConnectionHelp.Next(existing);
                adminHint.Text=string.IsNullOrEmpty(existing.AdminKey)?"读取用量需要组织管理员创建的管理密钥。":"已保存管理密钥。留空可保留。";
                showMessage(string.IsNullOrEmpty(existing.AdminKey)||string.IsNullOrEmpty(existing.ApiKeyId)?"OpenAI API Key 登录成功。补充管理密钥和对应 ID 后可查看 token 用量。":"OpenAI 已连接，用量按此 API Key ID 筛选。请确认 ID 对应上方普通密钥。");
            };
            Button save=null,choose=null;
            Action<bool> setBusy=value=>{Busy=value;returnButton.IsEnabled=!value;foreach(var tab in tabs.Values)tab.IsEnabled=!value;orbSize.IsEnabled=!value;api.IsEnabled=!value;admin.IsEnabled=!value;idInput.IsEnabled=!value;source.IsEnabled=!value;input.IsEnabled=!value&&source.SelectedIndex==0;appearance.IsEnabled=!value;if(save!=null)save.IsEnabled=!value;if(choose!=null)choose.IsEnabled=!value;};
            var verify=UI.Button("验证并保存普通密钥",async()=>{
                if(Busy)return;setBusy(true);
                try {var c=compose();c.AdminKey=null;c.ApiKeyId=null;await app.VerifyOpenAiKey(c.ApiKey);existing=OpenAiSecretStore.Read();loginHint.Text="密钥验证成功。留空可保留。";connectionState.Text=app.OpenAiData!=null?"历史用量已连接":OpenAiConnectionHelp.Next(existing);showMessage(connectionState.Text);}
                catch(Exception e){showMessage(ConnectionError(e));}finally{setBusy(false);}
            });verify.HorizontalAlignment=HorizontalAlignment.Left;verify.Margin=new Thickness(0,0,0,20);Body.Children.Insert(3,verify);
            var keyPage=UI.Button("密钥管理 ↗",()=>UI.Link("https://platform.openai.com/api-keys"));keyPage.Margin=new Thickness(8,0,0,0);
            var connectionActions=new WrapPanel {Margin=new Thickness(0,0,0,12)};connectionActions.Children.Add(keyPage);Body.Children.Add(connectionActions);footer.Children.Add(message);
            var info=UI.Text("密钥由 Windows 当前用户加密保存在本机。ChatGPT 自动使用本机 Codex 的登录账号。",11.5,UI.Muted);info.Margin=new Thickness(2,0,0,14);Body.Children.Add(info);
            Body=pages["数据目录"];
            var storageLabel=UI.Text("数据目录",11,UI.Muted);storageLabel.Margin=new Thickness(2,0,0,5);Body.Children.Add(storageLabel);
            var storagePath=new TextBox {Text=Paths.Data,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,BorderThickness=new Thickness(0),Background=Brushes.Transparent,FontSize=11,Margin=new Thickness(2,0,0,8)};
            Appearance.Paint(storagePath,TextBox.ForegroundProperty,UI.Muted);Body.Children.Add(storagePath);
            var folder=UI.Button("打开数据目录",()=>{try {Directory.CreateDirectory(Paths.Data);Process.Start(new ProcessStartInfo(Paths.Data){UseShellExecute=true});}catch(Exception e){message.Text=LocalStorage.Explain(e,"打开数据目录");message.Visibility=Visibility.Visible;} });folder.HorizontalAlignment=HorizontalAlignment.Left;folder.Margin=new Thickness(0,0,0,14);Body.Children.Add(folder);
            choose=UI.Button("选择数据目录",async()=>{
                using(var dialog=new System.Windows.Forms.FolderBrowserDialog {Description="请选择空文件夹。现有配置和密钥会复制过去，原目录保留备份。",SelectedPath=Paths.Data,ShowNewFolderButton=true}) {
                    if(dialog.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;
                    setBusy(true);message.Text="正在等待同步完成并迁移数据…";message.Visibility=Visibility.Visible;
                    try {await app.ChangeDataDirectory(dialog.SelectedPath);storagePath.Text=Paths.Data;message.Text="数据目录已保存，重启后继续使用。原目录副本保留。";}
                    catch(Exception e) {message.Text=e is ArgumentException?e.Message:LocalStorage.Explain(e,"切换数据目录")+"\n原目录仍在使用，目标中的副本可保留或另选空目录。";}
                    finally {setBusy(false);}
                }
            });choose.HorizontalAlignment=HorizontalAlignment.Left;choose.Margin=new Thickness(0,0,0,14);
            var locationActions=new WrapPanel();Body.Children.Remove(folder);folder.Margin=new Thickness(0,0,8,14);locationActions.Children.Add(folder);locationActions.Children.Add(choose);Body.Children.Add(locationActions);
            var locationHint=UI.Text("选择空文件夹，自动迁移当前配置和密钥。",10.5,UI.Muted);locationHint.Margin=new Thickness(2,0,0,14);Body.Children.Add(locationHint);
            save=UI.Button("保存",async()=>{
                int previousScale=app.Prefs.OrbScale;
                string operation="保存密钥",previous=app.Prefs.Appearance,previousSource=app.Prefs.DeepSource;
                setBusy(true);
                try {
                    if(SelectedPage=="OpenAI 密钥") {operation="连接 OpenAI";showMessage("正在验证 OpenAI API Key…");await connectCurrent();}
                    operation="保存密钥";
                    if(SelectedPage=="DeepSeek 密钥"&&source.SelectedIndex==0&&input.Password.Length>0)SecretStore.Save(input.Password);
                    if(SelectedPage=="DeepSeek 密钥"){app.Prefs.DeepSource=source.SelectedIndex==1?"harness":"api";app.Prefs.MonitorDeep=true;}
                    operation="保存设置";if(SelectedPage=="外观设置"){app.Prefs.Appearance=new[]{"system","light","dark"}[Math.Max(0,appearance.SelectedIndex)];app.Prefs.OrbScale=OrbView.Scales[Math.Max(0,orbSize.SelectedIndex)];}app.Prefs.Save();app.Orb.ApplyScale();
                } catch(Exception e) {
                    app.Prefs.OrbScale=previousScale;app.Prefs.DeepSource=previousSource;app.Prefs.Appearance=previous;if(operation=="连接 OpenAI"&&app.OpenAiKeyValidated){loginHint.Text="密钥验证成功";connectionState.Text="历史用量未连接 · "+ConnectionError(e);}
                    showMessage(operation=="连接 OpenAI"?ConnectionError(e):e is ArgumentException?e.Message:LocalStorage.Explain(e,operation));return;
                }
                finally {setBusy(false);}
                if(previousSource!=app.Prefs.DeepSource){app.DeepData=null;app.DeepError=null;app.Deep.Render(null,null,false);}
                if(SelectedPage=="DeepSeek 密钥")input.Clear();LocalStorage.Notice=null;Appearance.Apply(app.Prefs.Appearance);if(SelectedPage!="OpenAI 密钥")showMessage("已保存"+SelectedPage+"。");if(SelectedPage=="DeepSeek 密钥"){app.ResetDeepCooldown();await app.RefreshDeep(true);}
            });Appearance.Paint(save,Button.BackgroundProperty,UI.Green);save.Foreground=Brushes.White;
            save.Content="保存密钥设置";saveButton=save;save.HorizontalAlignment=HorizontalAlignment.Left;footer.Children.Add(save);
            Body=pages["关于软件"];var aboutLogo=Brand.Mark(64);aboutLogo.HorizontalAlignment=HorizontalAlignment.Left;Body.Children.Add(aboutLogo);Body.Children.Add(UI.Text("Token Monitor",23,UI.Ink,true));Body.Children.Add(UI.Text("版本 v3.2.0 · Windows 桌面用量工具",13,UI.Muted));
            var about=UI.Text("ChatGPT 悬浮球展示 Codex 与 ChatGPT Work 共用的额度。独立查看 DeepSeek 余额及 OpenAI API 用量。",13,UI.Muted);about.Margin=new Thickness(0,20,0,24);Body.Children.Add(about);
            var updateInfo=UI.Text("",12,UI.Muted);updateInfo.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Text"){Source=app.Main.UpdateStatus});Body.Children.Add(updateInfo);
            var aboutActions=new WrapPanel {Margin=new Thickness(0,14,0,18)};
            var check=UI.Button("检查更新",async()=>await app.CheckUpdates(true));check.SetBinding(Button.IsEnabledProperty,new System.Windows.Data.Binding("IsEnabled"){Source=app.Main.CheckUpdate});aboutActions.Children.Add(check);
            var release=UI.Button("发布页 ↗",async()=>await app.InstallUpdate());release.SetBinding(Button.IsEnabledProperty,new System.Windows.Data.Binding("IsEnabled"){Source=app.Main.DownloadUpdate});release.SetBinding(Button.ContentProperty,new System.Windows.Data.Binding("Content"){Source=app.Main.DownloadUpdate});release.Margin=new Thickness(8,0,0,0);aboutActions.Children.Add(release);Body.Children.Add(aboutActions);
            var releasePage=UI.Button("手动下载 / 发布页 ↗",app.OpenUpdate);releasePage.Margin=new Thickness(0,0,0,16);releasePage.HorizontalAlignment=HorizontalAlignment.Left;Body.Children.Add(releasePage);
            var rollback=UI.Button("回退到上一版",()=>app.RollbackUpdate());rollback.Margin=new Thickness(0,0,0,16);rollback.HorizontalAlignment=HorizontalAlignment.Left;rollback.IsEnabled=UpdateInstaller.CanRollback;Body.Children.Add(rollback);
            var repository=UI.Button("GitHub 源码 ↗",()=>UI.Link("https://github.com/Ricki0613/TokenMonitor"));repository.HorizontalAlignment=HorizontalAlignment.Left;Body.Children.Add(repository);Body.Children.Add(UI.Text("MIT License · Ricki0613",11,UI.Muted));
            SelectPage("外观设置");
        }
        public void SelectPage(string name) {
            if(Busy||!pages.ContainsKey(name))return;SelectedPage=name;heading.Text=name;
            foreach(var pair in pages)pair.Value.Visibility=pair.Key==name?Visibility.Visible:Visibility.Collapsed;
            foreach(var pair in tabs){pair.Value.SetResourceReference(Button.BackgroundProperty,pair.Key==name?"ButtonBrush":"SurfaceBrush");pair.Value.SetResourceReference(Button.ForegroundProperty,pair.Key==name?"InkBrush":"MutedBrush");}
            if(saveButton!=null)saveButton.Visibility=name=="DeepSeek 密钥"||name=="OpenAI 密钥"?Visibility.Visible:Visibility.Collapsed;
            pageHint.Text=name=="外观设置"?"修改后立即生效并自动保存。":name=="数据目录"?"选择空目录并完成迁移后立即生效，原目录副本保留。":name=="关于软件"?"版本信息、更新与开源项目。":"填写后点击保存密钥设置；切换分类保留输入，返回概览放弃未保存的修改。";
            if(feedback!=null){string notice=name=="OpenAI 密钥"?openAiReadIssue:name=="数据目录"?LocalStorage.Notice:null;feedback.Text=notice??"";feedback.Visibility=string.IsNullOrEmpty(notice)?Visibility.Collapsed:Visibility.Visible;}
            scroll.ScrollToHome();
        }
        public void ClearSecrets(){foreach(var panel in pages.Values)foreach(var child in panel.Children.OfType<PasswordBox>())child.Clear();}
        static PasswordBox PasswordInput(string name) {
            var input=new PasswordBox {Height=37,Padding=new Thickness(10,7,10,7),FontSize=14,BorderThickness=new Thickness(1)};
            AutomationProperties.SetName(input,name);return input;
        }
        static string ConnectionError(Exception error) {
            if(error is ProviderException||error is ArgumentException)return error.Message;
            if(error is System.Net.Http.HttpRequestException||error is System.Threading.Tasks.TaskCanceledException)return "无法连接 OpenAI，请检查网络后重试。输入内容已保留。";
            return LocalStorage.Explain(error,"连接 OpenAI");
        }
    }
}
