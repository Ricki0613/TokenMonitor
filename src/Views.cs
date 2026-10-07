using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TokenMonitor {
    public static class UI {
        public const string Green="#10A37F", Blue="#4D6BFE", Muted="#7B838D", Ink="#202526";
        public static SolidColorBrush Brush(string hex) { return (SolidColorBrush)new BrushConverter().ConvertFromString(hex); }
        public static TextBlock Text(string s,double size=13,string color=Ink,bool bold=false) {
            return new TextBlock { Text=s,FontSize=size,Foreground=Brush(color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,TextWrapping=TextWrapping.Wrap };
        }
        public static Button Button(string s,Action action,string tip=null) {
            var b=new Button { Content=s,ToolTip=tip };b.Click+=(o,e)=>action();return b;
        }
        public static Border Card(UIElement child,int padding=16) {
            return new Border { Background=Brush("#FFFFFF"),CornerRadius=new CornerRadius(12),Padding=new Thickness(padding),Child=child,Margin=new Thickness(0,0,0,10) };
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
        public Action CloseAction;
        public bool Exiting;
        public Shell(string title,string sub,string accent,double width,double height,bool floating) {
            Title=title+" · Token Monitor";Width=width;Height=height;Topmost=floating;ShowInTaskbar=!floating;
            WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
            FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI");
            UseLayoutRounding=true;SnapsToDevicePixels=true;
            var iconPath=System.IO.Path.Combine(Paths.Root,"TokenMonitor.ico");
            if(File.Exists(iconPath)) Icon=BitmapFrame.Create(new Uri(iconPath));
            Surface=new Border { Background=UI.Brush("#F5F7F8"),BorderBrush=UI.Brush("#E2E6E8"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(18),Margin=new Thickness(12),
                Effect=new DropShadowEffect { BlurRadius=18,ShadowDepth=3,Opacity=0.12,Color=Colors.Black } };
            Content=Surface;
            var layout=new DockPanel { LastChildFill=true };Surface.Child=layout;
            var titleArea=new Border { Padding=new Thickness(19,17,14,14),Background=Brushes.Transparent,Cursor=Cursors.SizeAll };
            titleArea.MouseLeftButtonDown+=(s,e)=> { if(e.ChangedButton==MouseButton.Left) { try {DragMove();} catch {} } };
            var mark=new Border { Background=UI.Brush(accent),Width=7,Height=7,CornerRadius=new CornerRadius(4),Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Center };
            var heading=new StackPanel {Orientation=Orientation.Horizontal};heading.Children.Add(mark);heading.Children.Add(UI.Text(title,14,UI.Ink,true));
            var words=new StackPanel();words.Children.Add(heading);Subtitle=UI.Text(sub,10.5,UI.Muted);Subtitle.Margin=new Thickness(15,4,0,0);words.Children.Add(Subtitle);
            var close=UI.Button("×",()=>{if(CloseAction!=null)CloseAction();else Close();},floating?"隐藏此悬浮窗":"收起到托盘");
            close.FontSize=20;close.Padding=new Thickness(8,0,8,2);close.Background=Brushes.Transparent;close.VerticalAlignment=VerticalAlignment.Top;close.Cursor=Cursors.Hand;
            AutomationProperties.SetName(close,floating?"隐藏"+title:"收起控制面板");
            titleArea.Child=UI.Row(words,close);DockPanel.SetDock(titleArea,Dock.Top);layout.Children.Add(titleArea);
            Scroll=new ScrollViewer { VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(18,0,18,16),Content=Body };
            layout.Children.Add(Scroll);
            Closing+=(s,e)=>{ if(!Exiting) {e.Cancel=true;if(CloseAction!=null)CloseAction();else Hide();} };
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
        public readonly CheckBox GptSwitch,DeepSwitch;
        public readonly TextBlock GptInfo,DeepInfo;
        public readonly ComboBox Interval;
        public readonly Button Refresh;
        public MainView(MonitorApp app):base("Token Monitor","用量，随时可见",UI.Green,410,398,false) {
            GptInfo=UI.Text("正在连接当前账号…",11.5,UI.Muted);
            DeepInfo=UI.Text("正在查询 API 余额…",11.5,UI.Muted);
            GptSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Gpt };
            DeepSwitch=new CheckBox { Style=(Style)Application.Current.FindResource("Switch"),IsChecked=app.Prefs.Deep };
            AutomationProperties.SetName(GptSwitch,"显示 GPT 用量悬浮窗");AutomationProperties.SetName(DeepSwitch,"显示 DeepSeek 用量悬浮窗");
            Body.Children.Add(ServiceCard("ChatGPT", "Codex / Work",GptInfo,GptSwitch,UI.Green));
            Body.Children.Add(ServiceCard("DeepSeek", "API",DeepInfo,DeepSwitch,UI.Blue));
            GptSwitch.Checked+=(s,e)=>app.SetGpt(true);GptSwitch.Unchecked+=(s,e)=>app.SetGpt(false);
            DeepSwitch.Checked+=(s,e)=>app.SetDeep(true);DeepSwitch.Unchecked+=(s,e)=>app.SetDeep(false);
            Interval=new ComboBox { Width=85,Height=28,FontSize=12,VerticalContentAlignment=VerticalAlignment.Center,ItemsSource=new[]{"15 秒","30 秒","60 秒","120 秒"} };
            Interval.SelectedIndex=Array.IndexOf(new[]{15,30,60,120},app.Prefs.Interval);
            AutomationProperties.SetName(Interval,"自动刷新间隔");
            Interval.SelectionChanged+=(s,e)=>{app.Prefs.Interval=new[]{15,30,60,120}[Interval.SelectedIndex];app.Save();app.UpdateLabels();};
            var auto=UI.Row(UI.Text("自动刷新",12,UI.Muted),Interval);auto.Margin=new Thickness(2,3,2,16);Body.Children.Add(auto);
            Refresh=UI.Button("立即刷新",async()=>await app.RefreshAll(true));Refresh.Background=UI.Brush(UI.Ink);Refresh.Foreground=Brushes.White;
            var actions=new StackPanel { Orientation=Orientation.Horizontal };
            actions.Children.Add(Refresh);var settings=UI.Button("设置",()=>new SettingsView(app){Owner=this}.ShowDialog());settings.Margin=new Thickness(8,0,0,0);actions.Children.Add(settings);
            var exit=UI.Button("退出",()=>app.Quit());Body.Children.Add(UI.Row(actions,exit));
            var note=UI.Text("悬浮窗可拖动 · 关闭面板后仍在托盘运行",10.5,UI.Muted);note.Margin=new Thickness(1,13,0,0);Body.Children.Add(note);
            CloseAction=()=>Hide();
        }
        static Border ServiceCard(string name,string tag,TextBlock info,CheckBox toggle,string color) {
            var words=new StackPanel();var header=new StackPanel { Orientation=Orientation.Horizontal };
            header.Children.Add(UI.Text(name,15,UI.Ink,true));
            var label=UI.Text(tag,10,color);label.Margin=new Thickness(8,4,0,0);header.Children.Add(label);words.Children.Add(header);info.Margin=new Thickness(0,7,0,0);words.Children.Add(info);
            return UI.Card(UI.Row(words,toggle));
        }
    }
    public sealed class UsageView : Shell {
        readonly MonitorApp app;
        public readonly bool Gpt;
        readonly StackPanel details=new StackPanel();
        readonly TextBlock status=UI.Text("正在连接…",10.5,UI.Muted);
        readonly TextBlock interval=UI.Text("",10,UI.Muted);
        readonly List<Tuple<TextBlock,QuotaWindow>> clocks=new List<Tuple<TextBlock,QuotaWindow>>();
        public readonly Button Refresh;
        public UsageView(MonitorApp owner,bool gpt):base(gpt?"ChatGPT":"DeepSeek",gpt?"Codex / ChatGPT Work":"API 账户余额",gpt?UI.Green:UI.Blue,368,gpt?500:445,true) {
            app=owner;Gpt=gpt;Body.Children.Add(details);
            var buttons=new StackPanel { Orientation=Orientation.Horizontal };
            Refresh=UI.Button("刷新",async()=>{if(Gpt)await app.RefreshGpt(true);else await app.RefreshDeep(true);});buttons.Children.Add(Refresh);
            var official=UI.Button("官方用量 ↗",()=>UI.Link(Gpt?"https://chatgpt.com/codex/settings/usage":"https://platform.deepseek.com/usage"));official.Margin=new Thickness(6,0,0,0);buttons.Children.Add(official);
            var controls=UI.Button("⋯",()=>app.ShowControl(),"打开控制面板");controls.FontSize=16;controls.Padding=new Thickness(12,3,12,3);
            Body.Children.Add(UI.Row(buttons,controls));
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
                foreach(var w in group.Windows) {
                    var box=new StackPanel();
                    var pct=new StackPanel {Orientation=Orientation.Horizontal};pct.Children.Add(UI.Text(w.Remaining.HasValue?w.Remaining.Value.ToString("0.#")+"%":"—",25,UI.Ink,true));
                    var remaining=UI.Text(" 剩余",10.5,UI.Muted);remaining.VerticalAlignment=VerticalAlignment.Bottom;remaining.Margin=new Thickness(0,0,0,4);pct.Children.Add(remaining);
                    var label=UI.Text(w.Label,12.5,UI.Ink,true);label.VerticalAlignment=VerticalAlignment.Center;box.Children.Add(UI.Row(label,pct));
                    var track=new Grid {Height=5,Margin=new Thickness(0,12,0,10)};
                    track.Children.Add(new Border {Background=UI.Brush("#E9EEEB"),CornerRadius=new CornerRadius(3)});
                    if(w.Remaining.HasValue) {
                        double remain=Math.Max(0,Math.Min(100,w.Remaining.Value));
                        var bar=new Border { Background=UI.Brush(remain<=10?"#DD6A42":UI.Green),CornerRadius=new CornerRadius(3),HorizontalAlignment=HorizontalAlignment.Left };
                        track.SizeChanged+=(s,e)=>bar.Width=track.ActualWidth*remain/100;track.Children.Add(bar);
                    }
                    box.Children.Add(track);var reset=UI.Text("",10.5,UI.Muted);box.Children.Add(reset);clocks.Add(Tuple.Create(reset,w));details.Children.Add(UI.Card(box));
                }
                if(group.Credits!=null) {var credits=UI.Row(UI.Text("额外 credits",11.5,UI.Muted),UI.Text(group.Credits,11.5,UI.Ink,true));credits.Margin=new Thickness(2,1,2,10);details.Children.Add(credits);}
            }
            if(snapshot.ResetCredits.HasValue && snapshot.ResetCredits.Value>0) {
                var reset=UI.Text("可用额度重置："+snapshot.ResetCredits.Value+" 次",10.5,UI.Muted);reset.Margin=new Thickness(2,0,0,12);details.Children.Add(reset);
            }
            UpdateClocks();
        }
        void RenderDeep(DeepSnapshot snapshot) {
            foreach(var b in snapshot.Balances) {
                var box=new StackPanel();box.Children.Add(UI.Row(UI.Text("可用余额",12,UI.Muted),UI.Text(b.Currency,10.5,UI.Blue,true)));
                var big=UI.Text(b.Format(b.Total),36,UI.Ink,true);big.Margin=new Thickness(0,10,0,12);box.Children.Add(big);
                box.Children.Add(new Border { Height=1,Background=UI.Brush("#EEF0F3"),Margin=new Thickness(0,0,0,14) });
                box.Children.Add(UI.Row(UI.Text("充值余额",12,UI.Muted),UI.Text(b.Format(b.ToppedUp),13,UI.Ink,true)));
                var gift=UI.Row(UI.Text("赠送余额",12,UI.Muted),UI.Text(b.Format(b.Granted),13,UI.Ink,true));gift.Margin=new Thickness(0,9,0,0);box.Children.Add(gift);details.Children.Add(UI.Card(box));
            }
            var available=UI.Text(snapshot.Available?"●  余额充足，可调用 API":"●  余额不足，暂不可调用 API",11,snapshot.Available?UI.Blue:"#CA6547");available.Margin=new Thickness(2,1,0,9);details.Children.Add(available);
            var note=UI.Text("Token 用量与消费明细见官方用量页",10.5,UI.Muted);note.Margin=new Thickness(2,0,0,14);details.Children.Add(note);
        }
        public void UpdateStatus(object data,string error,bool loading) {
            DateTimeOffset? fetched=data is GptSnapshot?(DateTimeOffset?)((GptSnapshot)data).Fetched:data is DeepSnapshot?(DateTimeOffset?)((DeepSnapshot)data).Fetched:null;
            status.Foreground=UI.Brush(error!=null?"#B77436":UI.Muted);
            status.Text=error!=null?(fetched.HasValue?"旧数据 · ":"")+error:loading?"正在刷新…":fetched.HasValue?"已同步 "+UI.Clock(fetched.Value):"等待同步";
            interval.Text=error!=null&&fetched.HasValue?"上次成功 "+UI.China(fetched.Value).ToString("MM-dd HH:mm:ss")+" · 北京时间":"每 "+app.Prefs.Interval+" 秒同步 · 北京时间";
            Refresh.IsEnabled=!loading;UpdateClocks();
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
    public sealed class SettingsView : Shell {
        public SettingsView(MonitorApp app):base("设置","连接与本机配置",UI.Green,408,376,false) {
            var label=UI.Text("DeepSeek API Key",13,UI.Ink,true);label.Margin=new Thickness(2,3,0,10);Body.Children.Add(label);
            var input=new PasswordBox {Height=37,Padding=new Thickness(10,7,10,7),FontSize=14,BorderBrush=UI.Brush("#DCE1E6"),BorderThickness=new Thickness(1)};
            AutomationProperties.SetName(input,"新的 DeepSeek API Key");Body.Children.Add(input);
            var hint=UI.Text(File.Exists(Paths.Key)?"已保存密钥。留空可保留原密钥。":"尚未设置密钥。",11,UI.Muted);hint.Margin=new Thickness(2,9,0,15);Body.Children.Add(hint);
            var info=UI.Text("密钥由 Windows 当前用户加密保存。GPT 自动使用本机 Codex 的登录账号。",12,UI.Muted);info.Margin=new Thickness(2,0,0,14);Body.Children.Add(info);
            var message=UI.Text("",11,"#B77436");message.Margin=new Thickness(2,0,0,10);Body.Children.Add(message);
            var save=UI.Button("保存",()=>{
                try {if(input.Password.Length>0)SecretStore.Save(input.Password);input.Clear();app.ResetDeepCooldown();Exiting=true;Close();app.RefreshAll(true);}
                catch(ArgumentException e){message.Text=e.Message;}catch{message.Text="保存失败，请检查安装目录的写入权限。";}
            });save.Background=UI.Brush(UI.Green);save.Foreground=Brushes.White;
            var cancel=UI.Button("取消",()=>{Exiting=true;Close();});Body.Children.Add(UI.Row(save,cancel));
            CloseAction=()=>{Exiting=true;Close();};WindowStartupLocation=WindowStartupLocation.CenterOwner;
        }
    }
}
