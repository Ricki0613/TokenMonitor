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
            if(color==Muted&&size<=13)size=11;
            else if(color==Ink&&size>=11.5&&size<=14)size=13;
            var text=new TextBlock { Text=s,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap };
            FontSizing.Bind(text,TextBlock.FontSizeProperty,size);Appearance.Paint(text,TextBlock.ForegroundProperty,color);return text;
        }
        public static TextBlock Section(string text) {return Text(text,16,Ink,true);}
        public static TextBlock Field(string text) {return Text(text,13,Ink,true);}
        public static TextBlock Hint(string text) {return Text(text,11,Muted);}
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
            WindowStyle=WindowStyle.None;ResizeMode=formal?ResizeMode.CanMinimize:floating?ResizeMode.CanResize:ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
            MinWidth=floating?180:width;MinHeight=floating?48:100;
            FontFamily=(FontFamily)Application.Current.FindResource("InterfaceFont");
            UseLayoutRounding=true;SnapsToDevicePixels=true;
            var iconPath=System.IO.Path.Combine(Paths.Root,"TokenMonitor.ico");
            if(File.Exists(iconPath)) Icon=BitmapFrame.Create(new Uri(iconPath));
            Surface=new Border { BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(18),Margin=new Thickness(12),
                Effect=new DropShadowEffect { BlurRadius=18,ShadowDepth=3,Opacity=0.12,Color=Colors.Black } };
            Appearance.Paint(Surface,Border.BackgroundProperty,"#F5F7F8");Appearance.Paint(Surface,Border.BorderBrushProperty,"#E2E6E8");
            if(formal) {Surface.Margin=new Thickness(0);Surface.CornerRadius=new CornerRadius(14);Surface.Effect=null;MinWidth=760;MinHeight=560;AutoHeight=false;}
            var root=new Grid();root.Children.Add(Surface);Content=root;
            var layout=new DockPanel { LastChildFill=true };ContentPanel=layout;Surface.Child=layout;
            SourceInitialized+=(s,e)=>Appearance.ApplyTitleBar(this);
            titleArea=new Border { Padding=new Thickness(19,17,14,14),Background=Brushes.Transparent,Cursor=Cursors.SizeAll };
            titleArea.MouseLeftButtonDown+=(s,e)=> { if(e.ChangedButton==MouseButton.Left && !e.Handled) { try {DragMove();} catch {} } };
            var mark=new Border { Background=UI.Brush(accent),Width=7,Height=7,CornerRadius=new CornerRadius(4),Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Center };
            FrameworkElement brand=formal?Brand.Mark(28):mark;brand.Margin=new Thickness(0,0,8,0);brand.VerticalAlignment=VerticalAlignment.Center;if(formal)AutomationProperties.SetName(brand,"Token Monitor 标志");
            var heading=new StackPanel {Orientation=Orientation.Horizontal};heading.Children.Add(brand);heading.Children.Add(UI.Text(title,14,UI.Ink,true));
            var words=new StackPanel();words.Children.Add(heading);Subtitle=UI.Text(sub,10.5,UI.Muted);Subtitle.Margin=new Thickness(formal?36:15,4,0,0);words.Children.Add(Subtitle);
            var close=UI.Button("×",()=>{if(CloseAction!=null)CloseAction();else Close();},floating?"隐藏此悬浮窗":"收起到托盘");
            close.FontSize=20;close.Padding=new Thickness(8,0,8,2);close.Background=Brushes.Transparent;close.VerticalAlignment=VerticalAlignment.Top;close.Cursor=Cursors.Hand;
            AutomationProperties.SetName(close,floating?"隐藏"+title:"收起控制面板");
            var titleButtons=new StackPanel { Orientation=Orientation.Horizontal };TitleButtons=titleButtons;
            if(floating){
                Surface.Margin=new Thickness(3);
                Surface.Effect=null;
                Surface.CornerRadius=new CornerRadius(10);
                titleArea.Padding=new Thickness(10,3,5,2);
                Subtitle.Visibility=Visibility.Collapsed;
            }
            if(formal) {
                var minimize=UI.Button("",()=>WindowState=WindowState.Minimized,"最小化");
                SetCaptionIcon(minimize,"M2,7 L12,7","最小化");
                SetCaptionIcon(close,"M3,3 L11,11 M11,3 L3,11","关闭窗口");
                close.ToolTip="关闭窗口，继续在托盘运行";
                titleButtons.Children.Add(minimize);
            }
            if(!floating)titleButtons.Children.Add(close);
            titleArea.Child=UI.Row(words,titleButtons);DockPanel.SetDock(titleArea,Dock.Top);layout.Children.Add(titleArea);
            Scroll=new ScrollViewer { VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(18,0,18,16),Content=Body };
            if(floating){Scroll.Padding=new Thickness(10,0,10,7);Scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Disabled;}
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
        static void SetCaptionIcon(Button button,string geometry,string label) {
            var icon=new System.Windows.Shapes.Path {Width=14,Height=14,Data=Geometry.Parse(geometry),StrokeThickness=1.6};
            icon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"InkBrush");
            button.Content=icon;
            button.Width=38;
            button.Height=34;
            button.Margin=button.Padding=new Thickness(0);
            button.VerticalAlignment=VerticalAlignment.Center;
            button.Background=Brushes.Transparent;
            AutomationProperties.SetName(button,label);
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
            double frame=2*(Surface.Margin.Left+Surface.BorderThickness.Left);
            double width=Math.Max(0,Width-frame);
            titleArea.Measure(new Size(width,double.PositiveInfinity));
            Body.Measure(new Size(Math.Max(0,width-Scroll.Padding.Left-Scroll.Padding.Right),double.PositiveInfinity));
            double needed=frame+titleArea.DesiredSize.Height+Body.DesiredSize.Height+Scroll.Padding.Top+Scroll.Padding.Bottom;
            double height=Math.Max(48,Math.Min(SystemParameters.WorkArea.Height,Math.Ceiling(needed)+1));
            MinHeight=height;
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
        public readonly CheckBox GptSwitch,DeepSwitch,OpenAiSwitch,KimiSwitch,KimiApiSwitch;
        public readonly Button ResetQuota;
        public readonly TextBlock GptInfo,DeepInfo,OpenAiInfo;
        public readonly ComboBox Interval;
        public readonly Button Refresh;
        public readonly Button CheckUpdate,DownloadUpdate;
        public readonly TextBlock UpdateStatus;
        readonly MonitorApp app;
        readonly Grid workspace=new Grid();
        readonly Dictionary<string,Button> navigation=new Dictionary<string,Button>();
        HistoryView history;
        readonly Grid services=new Grid();
        readonly List<Border> tiles=new List<Border>();
        public static readonly string[] Names={"ChatGPT 订阅","DeepSeek 余额","Kimi 订阅","Kimi API 余额","OpenAI API"};
        readonly DashboardCard kimiQuota=new DashboardCard(),kimiBalance=new DashboardCard();
        readonly TextBlock kimiQuotaStatus=UI.Hint("等待读取订阅用量"),kimiBalanceStatus=UI.Hint("尚未连接 Kimi API");
        string kimiLastSync;
        internal string ShowKimiUsage(QuotaWindow reading) {
            if(reading!=null){
                kimiQuota.RenderQuota(reading);
                kimiLastSync=UI.Clock(DateTimeOffset.UtcNow);
                kimiQuotaStatus.Text="已同步 "+kimiLastSync+" · 剩余额度";
            } else {
                kimiQuotaStatus.Text=kimiLastSync==null?"请打开 Kimi 桌面端「我的订阅」后刷新":"上次读取 "+kimiLastSync+" · 当前未同步";
            }
            app.DisplayKimiUsage(reading,kimiQuotaStatus.Text);
            return kimiQuotaStatus.Text;
        }
        internal string ShowKimiBalance(KimiBalance reading) {
            kimiBalance.RenderBalance(reading.Amount,reading.Cash,reading.Voucher);
            kimiBalanceStatus.Text=reading.Status;
            app.DisplayKimiBalance(reading);
            return reading.Status;
        }
        public void HomeVisibility() {
            services.Children.Clear();
            services.RowDefinitions.Clear();
            services.ColumnDefinitions.Clear();
            var visible=tiles.Where((tile,index)=>(app.Prefs.HiddenHome&(1<<index))==0).ToList();
            if(visible.Count==0){
                services.Children.Add(UI.Hint("可在「外观设置 → 主页展示」中勾选服务。"));
                return;
            }
            int[][] presets={new[]{1},new[]{2},new[]{3},new[]{2,2},new[]{3,2}};
            var rows=presets[visible.Count-1];
            const int columns=6;
            for(int col=0;col<columns;col++)services.ColumnDefinitions.Add(new ColumnDefinition());
            int nextTile=0;
            for(int row=0;row<rows.Length;row++){
                services.RowDefinitions.Add(new RowDefinition());
                int span=columns/rows[row];
                for(int col=0;col<rows[row];col++){
                    var tile=visible[nextTile++];
                    bool single=visible.Count==1;
                    var metric=(DashboardCard)tile.Tag;
                    metric.SetFocusLayout(single);
                    metric.VerticalAlignment=single?VerticalAlignment.Center:VerticalAlignment.Top;
                    tile.Padding=new Thickness(single?28:14);
                    Grid.SetRow(tile,row);
                    Grid.SetColumn(tile,col*span);
                    Grid.SetColumnSpan(tile,span);
                    tile.Margin=new Thickness(col==0?0:5,row==0?0:5,col==rows[row]-1?0:5,row==rows.Length-1?0:5);
                    services.Children.Add(tile);
                }
            }
        }

        void AddTile(string name, FrameworkElement metrics,string page,TextBlock status=null,CheckBox toggle=null,Action compact=null) {
            var content=new Grid();
            content.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            content.Children.Add(UI.Section(name));
            metrics.Margin=new Thickness(0,8,0,8);
            metrics.VerticalAlignment=VerticalAlignment.Top;
            Grid.SetRow(metrics,1);
            content.Children.Add(metrics);
            var details=new StackPanel();
            if(status!=null){status.Margin=new Thickness(0,0,0,8);details.Children.Add(status);}
            var actions=new WrapPanel();
            var connect=UI.Button("连接设置",()=>OpenSettings(page));

            actions.Children.Add(connect);
            if(compact!=null)actions.Children.Add(UI.Button("悬浮球",compact));
            if(toggle==GptSwitch)actions.Children.Add(ResetQuota);
            foreach(Button action in actions.Children)action.Margin=new Thickness(0,0,6,6);
            details.Children.Add(actions);
            if(toggle!=null){
                var row=UI.Row(UI.Text("显示浮窗",13),toggle);
                row.Margin=new Thickness(0,2,0,0);
                details.Children.Add(row);
            }
            Grid.SetRow(details,2);
            content.Children.Add(details);
            var tile=UI.Card(content,14);
            tile.Tag=metrics;
            AutomationProperties.SetName(tile,name);
            tiles.Add(tile);
        }
        readonly DashboardCard gptCard,deepCard,openCard;
        public SettingsView SettingsPage {get;private set;}
        public MainView(MonitorApp app):base("Token Monitor","用量控制中心  /  v4.0.0",UI.Green,1060,720,false,true) {
            this.app=app;
            ResizeMode=ResizeMode.CanMinimize;Width=Math.Min(1060,SystemParameters.WorkArea.Width);Height=Math.Min(720,SystemParameters.WorkArea.Height);MinWidth=Math.Min(760,Width);MinHeight=Math.Min(560,Height);
            ContentPanel.Children.Remove(Scroll);
            var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(170)});layout.ColumnDefinitions.Add(new ColumnDefinition());
            var nav=new StackPanel {Margin=new Thickness(14,4,10,14)};layout.Children.Add(nav);Grid.SetColumn(workspace,1);layout.Children.Add(workspace);workspace.Children.Add(Scroll);ContentPanel.Children.Add(layout);
            foreach(string name in new[]{"主页","历史趋势","外观设置","ChatGPT 连接","DeepSeek API","Kimi 订阅","Kimi API","OpenAI API","数据目录","关于软件"}) {string target=name;var button=UI.Button(name,()=>{if(target=="主页")ShowOverview();else if(target=="历史趋势")ShowHistory();else OpenSettings(target);});button.Margin=new Thickness(0,0,0,name=="历史趋势"?24:8);navigation.Add(name,button);nav.Children.Add(button);}
            SelectNavigation("主页");
            GptInfo=UI.Hint("选择连接设置，连接本机账号");
            DeepInfo=UI.Hint("选择连接设置，添加余额来源");
            OpenAiInfo=UI.Hint("选择连接设置，接入历史用量");
            GptSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Gpt };
            DeepSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Deep };
            OpenAiSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.OpenAi };
            AutomationProperties.SetName(GptSwitch,"显示 GPT 用量悬浮窗");AutomationProperties.SetName(DeepSwitch,"显示 DeepSeek 用量悬浮窗");
            AutomationProperties.SetName(OpenAiSwitch,"显示 OpenAI API 用量悬浮窗");
            gptCard=new DashboardCard();
            deepCard=new DashboardCard();
            openCard=new DashboardCard();
            ResetQuota=UI.Button("重置额度",async()=>await app.ResetGptQuota());
            KimiSwitch=new CheckBox {Style=GptSwitch.Style,IsChecked=app.Prefs.KimiFloat};
            KimiApiSwitch=new CheckBox {Style=GptSwitch.Style,IsChecked=app.Prefs.KimiApiFloat};
            AutomationProperties.SetName(KimiSwitch,"显示 Kimi 订阅浮窗");
            AutomationProperties.SetName(KimiApiSwitch,"显示 Kimi API 余额浮窗");
            KimiSwitch.Click+=(s,e)=>app.SetKimiVisible(KimiSwitch.IsChecked==true,false);
            KimiApiSwitch.Click+=(s,e)=>app.SetKimiVisible(KimiApiSwitch.IsChecked==true,true);
            AddTile(Names[0],gptCard,"ChatGPT 连接",GptInfo,GptSwitch,()=>{GptSwitch.IsChecked=true;app.SetOrbMode(true);});
            AddTile(Names[1],deepCard,"DeepSeek API",DeepInfo,DeepSwitch);
            kimiQuota.RenderQuota(new QuotaWindow {Label="订阅剩余额度"});
            AddTile(Names[2],kimiQuota,"Kimi 订阅",kimiQuotaStatus,KimiSwitch,()=>app.SetKimiOrbMode(true));
            kimiBalance.RenderBalance(null);
            AddTile(Names[3],kimiBalance,"Kimi API",kimiBalanceStatus,KimiApiSwitch);
            AddTile(Names[4],openCard,"OpenAI API",OpenAiInfo,OpenAiSwitch);
            var home=new Grid();
            home.SetBinding(FrameworkElement.MinHeightProperty,new System.Windows.Data.Binding("ViewportHeight"){Source=Scroll});
            home.RowDefinitions.Add(new RowDefinition());
            home.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            home.Children.Add(services);
            Scroll.Content=home;
            HomeVisibility();
            services.VerticalAlignment=VerticalAlignment.Stretch;
            services.Margin=new Thickness(0,0,0,16);
            GptSwitch.Checked+=(s,e)=>app.SetGpt(true);GptSwitch.Unchecked+=(s,e)=>app.SetGpt(false);
            DeepSwitch.Checked+=(s,e)=>app.SetDeep(true);DeepSwitch.Unchecked+=(s,e)=>app.SetDeep(false);
            OpenAiSwitch.Checked+=(s,e)=>app.SetOpenAi(true);OpenAiSwitch.Unchecked+=(s,e)=>app.SetOpenAi(false);
            Interval=new ComboBox { Width=85,Height=28,VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=new[]{"15 秒","30 秒","60 秒","120 秒"} };
            Interval.SelectedIndex=Array.IndexOf(new[]{15,30,60,120},app.Prefs.Interval);
            AutomationProperties.SetName(Interval,"自动刷新间隔");
            Interval.SelectionChanged+=(s,e)=>{app.Prefs.Interval=new[]{15,30,60,120}[Interval.SelectedIndex];app.Save();app.UpdateLabels();};
            var auto=new StackPanel {Orientation=Orientation.Horizontal};var autoLabel=UI.Text("自动刷新",12,UI.Muted);autoLabel.VerticalAlignment=VerticalAlignment.Center;autoLabel.Margin=new Thickness(0,0,12,0);auto.Children.Add(autoLabel);auto.Children.Add(Interval);auto.Margin=new Thickness(2,3,2,16);var footer=new StackPanel();Grid.SetRow(footer,1);home.Children.Add(footer);footer.Children.Add(auto);
            Refresh=UI.Button("立即刷新",async()=>await app.RefreshAll(true));Refresh.SetResourceReference(Button.BackgroundProperty,"ActionBrush");Refresh.SetResourceReference(Button.ForegroundProperty,"ActionInkBrush");
            Refresh.Margin=new Thickness(16,0,0,0);auto.Children.Add(Refresh);
            var note=UI.Text(app.FirstRun?"先选择要监控的服务，再完成连接。":"悬浮球可拖动 · 双击展开详情 · 右键打开菜单 · 关闭主界面后继续在托盘运行",10.5,UI.Muted);note.Margin=new Thickness(1,13,0,0);footer.Children.Add(note);
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
            SettingsPage.SelectPage(page);SelectNavigation(page);Subtitle.Text="设置  /  v4.0.0";
        }
        public void HistoryChanged() {if(history!=null&&workspace.Children.Contains(history))history.Render();}
        public void ShowHistory() {
            if(SettingsPage!=null&&SettingsPage.Busy)return;
            if(SettingsPage!=null){SettingsPage.ClearSecrets();SettingsPage=null;}
            history=new HistoryView(app);workspace.Children.Clear();workspace.Children.Add(history);SelectNavigation("历史趋势");Subtitle.Text="历史趋势  /  v4.0.0";
        }
        public void ShowOverview() {
            if(SettingsPage!=null){if(SettingsPage.Busy)return;SettingsPage.ClearSecrets();SettingsPage=null;}
            workspace.Children.Clear();workspace.Children.Add(Scroll);SelectNavigation("主页");Subtitle.Text="用量控制中心  /  v4.0.0";
        }
        public void RenderUpdate(AvailableRelease release,string message) {
            UpdateStatus.Text=(release==null?"":"发现新版本 "+release.Tag+"，可下载更新。")+(message==null?"":(release==null?"":"\n")+message)+(release==null||string.IsNullOrWhiteSpace(release.Notes)?"":"\n\n"+release.Notes);
            DownloadUpdate.Content=release==null?"发布页 ↗":"下载更新 ↗";
            FitHeight();
        }
        public void RenderMetrics() {gptCard.Render(app.GptData,0);deepCard.Render(app.DeepData,1);openCard.Render(app.OpenAiData,2);}
    }
    public sealed class SettingsView : UserControl {
        readonly Dictionary<string,StackPanel> pages=new Dictionary<string,StackPanel>();
        readonly Dictionary<string,Button> tabs=new Dictionary<string,Button>();
        readonly TextBlock heading=UI.Text("",24,UI.Ink,true);
        readonly TextBlock pageHint=UI.Text("切换分类会保留未保存的输入；返回主页会放弃未保存的修改。",11,UI.Muted);
        readonly ScrollViewer scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        Button saveButton;
        TextBlock feedback;
        readonly StackPanel feedbackFooter=new StackPanel();
        readonly StackPanel storageFeedback=new StackPanel();
        string openAiReadIssue;
        public bool Busy {get;private set;}
        public string SelectedPage {get;private set;}
        public ScrollViewer Scroll {get{return scroll;}}
        public SettingsView(MonitorApp app,Action back,bool embedded=false) {
            var root=new Grid {Margin=new Thickness(18,4,18,20)};root.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(170)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=root;
            var nav=new StackPanel {Margin=new Thickness(0,0,20,0)};root.Children.Add(nav);
            var returnButton=UI.Button("← 返回主页",()=>{if(!Busy)back();});returnButton.Margin=new Thickness(0,0,0,28);nav.Children.Add(returnButton);
            nav.Children.Add(UI.Text("设置",12,UI.Muted,true));
            var pageGrid=new Grid();scroll.Content=pageGrid;
            foreach(string name in new[]{"外观设置","ChatGPT 连接","DeepSeek API","Kimi 订阅","Kimi API","OpenAI API","数据目录","关于软件"}) {
                var panel=new StackPanel {HorizontalAlignment=HorizontalAlignment.Left,MaxWidth=name=="关于软件"?850:640};
                panel.SetBinding(WidthProperty,new System.Windows.Data.Binding("ViewportWidth"){Source=scroll});pages.Add(name,panel);pageGrid.Children.Add(panel);
                string target=name;var tab=UI.Button(name,()=>SelectPage(target));tab.Margin=new Thickness(0,6,0,0);tabs.Add(name,tab);nav.Children.Add(tab);
            }
            if(embedded){nav.Visibility=Visibility.Collapsed;root.ColumnDefinitions[0].Width=new GridLength(0);}
            var right=new DockPanel {LastChildFill=true,Margin=new Thickness(embedded?0:16,4,4,0)};Grid.SetColumn(right,1);root.Children.Add(right);
            var header=new StackPanel {Margin=new Thickness(0,0,0,22)};header.Children.Add(heading);pageHint.Margin=new Thickness(0,8,0,0);header.Children.Add(pageHint);DockPanel.SetDock(header,Dock.Top);right.Children.Add(header);
            right.Children.Add(scroll);
            StackPanel Body=pages["外观设置"];

            var appearance=new ComboBox {Height=36,ItemsSource=new[]{"随系统","浅色","深色"},SelectedIndex=Array.IndexOf(new[]{"system","light","dark"},app.Prefs.Appearance)};
            AutomationProperties.SetName(appearance,"外观主题");
            var appearanceRow=UI.Row(UI.Field("外观"),appearance);appearance.Width=160;appearanceRow.Margin=new Thickness(2,0,2,16);Body.Children.Add(appearanceRow);
            var fontSize=new ComboBox {Width=160,Height=36,ItemsSource=new[]{"小","中（默认）","大"},SelectedIndex=Array.IndexOf(new[]{"small","medium","large"},app.Prefs.FontSize)};
            AutomationProperties.SetName(fontSize,"界面字号");var fontRow=UI.Row(UI.Field("界面字号"),fontSize);fontRow.Margin=new Thickness(2,0,2,16);Body.Children.Add(fontRow);
            fontSize.SelectionChanged+=(s,e)=>{string old=app.Prefs.FontSize;try{app.Prefs.FontSize=new[]{"small","medium","large"}[Math.Max(0,fontSize.SelectedIndex)];app.Prefs.Save();FontSizing.Apply(app.Prefs.FontSize);}catch{app.Prefs.FontSize=old;FontSizing.Apply(old);if(feedback!=null){feedback.Text="字号未保存，请检查数据目录权限。";feedback.Visibility=Visibility.Visible;}}};
            var orbSize=new ComboBox {Width=160,Height=36,ItemsSource=new[]{"80% · 紧凑","100% · 约 2 cm","125% · 清晰","150% · 大号","200% · 特大"},SelectedIndex=Array.IndexOf(OrbView.Scales,app.Prefs.OrbScale)};
            AutomationProperties.SetName(orbSize,"ChatGPT 悬浮球大小");
            var orbRow=UI.Row(UI.Field("悬浮球大小"),orbSize);orbRow.Margin=new Thickness(2,0,2,12);Body.Children.Add(orbRow);
            var orbHint=UI.Text("100% 为 76 个逻辑像素；实际尺寸取决于显示器和 Windows 缩放。上下数值均为剩余额度。",11,UI.Muted);orbHint.Margin=new Thickness(2,0,2,16);Body.Children.Add(orbHint);
            var opacity=new Slider {Minimum=0,Maximum=100,Value=app.Prefs.FloatOpacity,TickFrequency=1,IsSnapToTickEnabled=true,Width=114,VerticalAlignment=VerticalAlignment.Center,IsMoveToPointEnabled=true};
            AutomationProperties.SetName(opacity,"浮窗不透明度");
            var percent=UI.Text(app.Prefs.FloatOpacity+"%");
            percent.Width=46;percent.TextAlignment=TextAlignment.Right;percent.VerticalAlignment=VerticalAlignment.Center;
            var opacityControl=new StackPanel {Orientation=Orientation.Horizontal,Height=36};
            opacityControl.Children.Add(opacity);opacityControl.Children.Add(percent);
            Body.Children.Add(UI.Row(UI.Field("浮窗不透明度"),opacityControl));
            Body.Children.Add(UI.Hint("最低档保留 45% 不透明度，确保浮窗内容可见。"));
            opacity.ValueChanged+=(sender,args)=>{app.ApplyFloatOpacity((int)opacity.Value);percent.Text=app.Prefs.FloatOpacity+"%";};
            Body.Children.Add(UI.Section("主页展示"));
            for(int i=0;i<MainView.Names.Length;i++){int bit=1<<i;var c=new CheckBox {Content=MainView.Names[i],IsChecked=(app.Prefs.HiddenHome&bit)==0,Margin=new Thickness(2,5,0,5)};c.SetResourceReference(Control.ForegroundProperty,"InkBrush");c.Click+=(o,e)=>{if(c.IsChecked==true)app.Prefs.HiddenHome&=~bit;else app.Prefs.HiddenHome|=bit;app.Save();app.Main.HomeVisibility();};Body.Children.Add(c);}
            Body=pages["Kimi 订阅"];Body.Children.Add(UI.Text("读取桌面端订阅用量",16,UI.Ink,true));Body.Children.Add(UI.Text("打开 Kimi → 设置 → 我的订阅，然后点击读取。",13,UI.Muted));var ks=UI.Hint("");Body.Children.Add(UI.Button("读取当前用量",async()=>{ks.Text=await app.UpdateKUsage();}));Body.Children.Add(ks);
            Body=pages["Kimi API"];Body.Children.Add(UI.Section("连接 Kimi 开放平台"));Body.Children.Add(UI.Field("Kimi API Key"));var kk=PasswordInput("Kimi API Key");Body.Children.Add(kk);var kb=UI.Hint("");Body.Children.Add(UI.Button("验证并保存",async()=>{string k=kk.Password;kk.Clear();kb.Text=await app.UpdateKBalance(k,true);}));Body.Children.Add(kb);
            Body=pages["ChatGPT 连接"];
            Body.Children.Add(UI.Text("使用本机 Codex 账号",16,UI.Ink,true));
            var chatHint=UI.Text("1. 在 Codex 中登录你的 ChatGPT 账号。\n2. 点击下方连接，读取 Codex / ChatGPT Work 共用额度。\n3. 返回主页，按需开启浮窗或悬浮球。",13,UI.Muted);chatHint.Margin=new Thickness(0,16,0,20);Body.Children.Add(chatHint);
            var chatState=UI.Text(app.GptData!=null?"已连接本机账号":"等待连接",12,UI.Muted);Body.Children.Add(chatState);
            var chatConnect=UI.Button("连接本机账号",async()=>{app.Prefs.MonitorGpt=true;app.Save();await app.RefreshGpt(true);chatState.Text=app.GptError??"已连接，返回主页查看额度。";});chatConnect.HorizontalAlignment=HorizontalAlignment.Left;chatConnect.Margin=new Thickness(0,16,0,0);Body.Children.Add(chatConnect);
            Body=pages["DeepSeek API"];
            var source=new ComboBox {Height=36,ItemsSource=new[]{"手动 API Key","DeepSeek Harness 登录账户"},SelectedIndex=app.Prefs.DeepSource=="harness"?1:0};
            AutomationProperties.SetName(source,"DeepSeek 余额来源");
            Body.Children.Add(UI.Text("DeepSeek 余额来源",16,UI.Ink,true));Body.Children.Add(source);
            var harnessHint=UI.Text("Harness 模式读取本机登录状态，无需填写 API Key。请先在 Harness 登录；凭据不会复制保存。",11,UI.Muted);harnessHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(harnessHint);
            var label=UI.Text("DeepSeek API Key",13,UI.Ink,true);label.Margin=new Thickness(2,3,0,10);Body.Children.Add(label);
            var input=PasswordInput("新的 DeepSeek API Key");Body.Children.Add(input);
            var hint=UI.Text(File.Exists(Paths.Key)?"已保存密钥。留空可保留原密钥。":"尚未设置密钥。",11,UI.Muted);hint.Margin=new Thickness(2,9,0,15);Body.Children.Add(hint);

            Action sourceChanged=()=>{input.IsEnabled=source.SelectedIndex==0;label.Visibility=hint.Visibility=input.Visibility=source.SelectedIndex==0?Visibility.Visible:Visibility.Collapsed;};
            source.SelectionChanged+=(s,e)=>sourceChanged();sourceChanged();
            Body=pages["OpenAI API"];
            OpenAiConnection existing=null;string readError=null;
            try {if(File.Exists(Paths.OpenAiKey))existing=OpenAiSecretStore.Read();}
            catch(Exception e) {readError=LocalStorage.Explain(e,"读取 OpenAI API")+" 请重新填写 OpenAI API Key。";}
            var openAiLabel=UI.Text("1  验证 API 密钥",16,UI.Ink,true);openAiLabel.Margin=new Thickness(2,3,0,10);Body.Children.Add(openAiLabel);
            var api=PasswordInput("新的 OpenAI API Key");Body.Children.Add(api);
            var loginHint=UI.Text(existing!=null?"已保存普通密钥。留空可保留。":"填写你调用 API 时使用的 sk- 密钥，点击下方验证。",11,UI.Muted);loginHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(loginHint);
            var adminLabel=UI.Text("2  连接历史用量 · Admin API Key",16,UI.Ink,true);adminLabel.Margin=new Thickness(2,0,0,8);Body.Children.Add(adminLabel);
            var admin=PasswordInput("新的 OpenAI Admin API Key");Body.Children.Add(admin);
            var adminHint=UI.Text(existing!=null&&!string.IsNullOrEmpty(existing.AdminKey)?"已保存管理密钥。留空可保留。":"读取用量需要组织管理员创建的管理密钥。",11,UI.Muted);adminHint.Margin=new Thickness(2,9,0,12);Body.Children.Add(adminHint);
            var idLabel=UI.Field("要统计的密钥标识 · API Key ID");idLabel.Margin=new Thickness(2,0,0,8);Body.Children.Add(idLabel);
            var idInput=new TextBox {Text=existing==null?"":existing.ApiKeyId??"",Height=37,Padding=new Thickness(10,7,10,7),BorderThickness=new Thickness(1)};
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
                    Appearance.Apply(app.Prefs.Appearance);app.Orb.ApplyScale();app.KimiOrb.ApplyScale();showMessage("已自动保存，即时生效。");
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
            var connectionActions=new WrapPanel {Margin=new Thickness(0,0,0,12)};connectionActions.Children.Add(keyPage);Body.Children.Add(connectionActions);
            var info=UI.Text("密钥由 Windows 当前用户加密保存在本机。ChatGPT 自动使用本机 Codex 的登录账号。",11.5,UI.Muted);info.Margin=new Thickness(2,0,0,14);Body.Children.Add(info);
            Body=pages["数据目录"];
            var storageLabel=UI.Text("数据目录",11,UI.Muted);storageLabel.Margin=new Thickness(2,0,0,5);Body.Children.Add(storageLabel);
            var storagePath=new TextBox {Text=Paths.Data,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,BorderThickness=new Thickness(0),Background=Brushes.Transparent,FontSize=11,Margin=new Thickness(2,0,0,8)};
            Appearance.Paint(storagePath,TextBox.ForegroundProperty,UI.Muted);Body.Children.Add(storagePath);Body.Children.Add(storageFeedback);
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
                    if(SelectedPage=="OpenAI API") {operation="连接 OpenAI";showMessage("正在验证 OpenAI API Key…");await connectCurrent();}
                    operation="保存密钥";
                    if(SelectedPage=="DeepSeek API"&&source.SelectedIndex==0&&input.Password.Length>0)SecretStore.Save(input.Password);
                    if(SelectedPage=="DeepSeek API"){app.Prefs.DeepSource=source.SelectedIndex==1?"harness":"api";app.Prefs.MonitorDeep=true;}
                    operation="保存设置";if(SelectedPage=="外观设置"){app.Prefs.Appearance=new[]{"system","light","dark"}[Math.Max(0,appearance.SelectedIndex)];app.Prefs.OrbScale=OrbView.Scales[Math.Max(0,orbSize.SelectedIndex)];}app.Prefs.Save();app.Orb.ApplyScale();app.KimiOrb.ApplyScale();
                } catch(Exception e) {
                    app.Prefs.OrbScale=previousScale;app.Prefs.DeepSource=previousSource;app.Prefs.Appearance=previous;if(operation=="连接 OpenAI"&&app.OpenAiKeyValidated){loginHint.Text="密钥验证成功";connectionState.Text="历史用量未连接 · "+ConnectionError(e);}
                    showMessage(operation=="连接 OpenAI"?ConnectionError(e):e is ArgumentException?e.Message:LocalStorage.Explain(e,operation));return;
                }
                finally {setBusy(false);}
                if(previousSource!=app.Prefs.DeepSource){app.DeepData=null;app.DeepError=null;app.Deep.Render(null,null,false);}
                if(SelectedPage=="DeepSeek API")input.Clear();LocalStorage.Notice=null;Appearance.Apply(app.Prefs.Appearance);if(SelectedPage!="OpenAI API")showMessage("已保存"+SelectedPage+"。");if(SelectedPage=="DeepSeek API"){app.ResetDeepCooldown();await app.RefreshDeep(true);}
            });Appearance.Paint(save,Button.BackgroundProperty,UI.Green);save.Foreground=Brushes.White;
            save.Content="保存连接设置";saveButton=save;save.HorizontalAlignment=HorizontalAlignment.Left;save.Margin=new Thickness(0,12,0,0);
            Body=pages["关于软件"];var aboutLogo=Brand.Mark(64);aboutLogo.HorizontalAlignment=HorizontalAlignment.Left;Body.Children.Add(aboutLogo);Body.Children.Add(UI.Text("Token Monitor",23,UI.Ink,true));Body.Children.Add(UI.Text("版本 v4.0.0 · Windows 桌面用量工具",13,UI.Muted));
            var about=UI.Text("查看 ChatGPT 和 Kimi 订阅额度、DeepSeek 和 Kimi API 余额，以及 OpenAI API 用量。",13,UI.Muted);about.Margin=new Thickness(0,20,0,24);Body.Children.Add(about);
            var updateInfo=UI.Text("",12,UI.Muted);updateInfo.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Text"){Source=app.Main.UpdateStatus});Body.Children.Add(updateInfo);
            var aboutActions=new WrapPanel {Margin=new Thickness(0,14,0,18)};
            var check=UI.Button("检查更新",async()=>await app.CheckUpdates(true));check.SetBinding(Button.IsEnabledProperty,new System.Windows.Data.Binding("IsEnabled"){Source=app.Main.CheckUpdate});aboutActions.Children.Add(check);
            var release=UI.Button("发布页 ↗",async()=>await app.InstallUpdate());release.SetBinding(Button.IsEnabledProperty,new System.Windows.Data.Binding("IsEnabled"){Source=app.Main.DownloadUpdate});release.SetBinding(Button.ContentProperty,new System.Windows.Data.Binding("Content"){Source=app.Main.DownloadUpdate});release.Margin=new Thickness(8,0,0,0);aboutActions.Children.Add(release);Body.Children.Add(aboutActions);
            var releasePage=UI.Button("手动下载 / 发布页 ↗",app.OpenUpdate);releasePage.Margin=new Thickness(0,0,0,16);releasePage.HorizontalAlignment=HorizontalAlignment.Left;aboutActions.Children.Add(releasePage);
            var rollback=UI.Button("回退到上一版",()=>app.RollbackUpdate());rollback.Margin=new Thickness(0,0,0,16);rollback.HorizontalAlignment=HorizontalAlignment.Left;rollback.IsEnabled=UpdateInstaller.CanRollback;aboutActions.Children.Add(rollback);
            var repository=UI.Button("GitHub 源码 ↗",()=>UI.Link("https://github.com/Ricki0613/TokenMonitor"));repository.HorizontalAlignment=HorizontalAlignment.Left;aboutActions.Children.Add(repository);foreach(Button action in aboutActions.Children)action.Margin=new Thickness(0,0,8,8);Body.Children.Add(UI.Text("MIT License · Ricki0613",11,UI.Muted));
            foreach(var page in pages.Values)ApplyFormSpacing(page);
            SelectPage("外观设置");
        }
        static void ApplyFormSpacing(StackPanel page) {
            FrameworkElement previous=null;
            foreach(FrameworkElement item in page.Children) {
                var text=item as TextBlock;
                bool heading=text!=null&&text.FontWeight==FontWeights.SemiBold;
                int gap=heading?20:item is Button||item is Grid?12:8;
                item.Margin=new Thickness(0,previous==null?0:gap,0,0);
                var button=item as Button;
                if(button!=null)button.HorizontalAlignment=HorizontalAlignment.Left;
                var row=item as Grid;
                if(row!=null){
                    row.HorizontalAlignment=HorizontalAlignment.Left;
                    row.ColumnDefinitions[0].Width=new GridLength(164);
                    foreach(var field in row.Children.OfType<TextBlock>())field.VerticalAlignment=VerticalAlignment.Center;
                }
                previous=item;
            }
        }
        public void SelectPage(string name) {
            if(Busy||!pages.ContainsKey(name))return;SelectedPage=name;heading.Text=name;
            foreach(var pair in pages)pair.Value.Visibility=pair.Key==name?Visibility.Visible:Visibility.Collapsed;
            foreach(var pair in tabs){pair.Value.SetResourceReference(Button.BackgroundProperty,pair.Key==name?"ButtonBrush":"SurfaceBrush");pair.Value.SetResourceReference(Button.ForegroundProperty,pair.Key==name?"InkBrush":"MutedBrush");}
            if(saveButton!=null)saveButton.Visibility=name=="DeepSeek API"||name=="OpenAI API"?Visibility.Visible:Visibility.Collapsed;
            pageHint.Text=name=="外观设置"?"修改后立即生效并自动保存。":name=="数据目录"?"选择空目录并完成迁移后立即生效，原目录副本保留。":name=="关于软件"?"版本信息、更新与开源项目。":"连接后可在主页查看用量或余额。";
            if(feedback!=null){
                var parent=feedback.Parent as Panel;
                if(parent!=null)parent.Children.Remove(feedback);
                (name=="数据目录"?storageFeedback:feedbackFooter).Children.Add(feedback);
                string notice=name=="OpenAI API"?openAiReadIssue:name=="数据目录"?LocalStorage.Notice:null;feedback.Text=notice??"";feedback.Visibility=string.IsNullOrEmpty(notice)?Visibility.Collapsed:Visibility.Visible;}
            var oldHost=feedbackFooter.Parent as Panel;
            if(oldHost!=null)oldHost.Children.Remove(feedbackFooter);
            pages[name].Children.Add(feedbackFooter);
            var oldPage=saveButton.Parent as Panel;
            if(oldPage!=null)oldPage.Children.Remove(saveButton);
            pages[name].Children.Add(saveButton);
            scroll.ScrollToHome();
        }
        public void ClearSecrets(){foreach(var panel in pages.Values)foreach(var child in panel.Children.OfType<PasswordBox>())child.Clear();}
        static PasswordBox PasswordInput(string name) {
            var input=new PasswordBox {Height=37,Padding=new Thickness(10,7,10,7),BorderThickness=new Thickness(1)};
            AutomationProperties.SetName(input,name);return input;
        }
        static string ConnectionError(Exception error) {
            if(error is ProviderException||error is ArgumentException)return error.Message;
            if(error is System.Net.Http.HttpRequestException||error is System.Threading.Tasks.TaskCanceledException)return "无法连接 OpenAI，请检查网络后重试。输入内容已保留。";
            return LocalStorage.Explain(error,"连接 OpenAI");
        }
    }
}
