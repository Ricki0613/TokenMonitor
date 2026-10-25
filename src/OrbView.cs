using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TokenMonitor {
    // A split token monogram: two offset facets joined by a pulse, shared by all surfaces.
    public static class Brand {
        public static FrameworkElement Mark(double size) {
            var canvas=new Canvas {Width=40,Height=40};
            canvas.Children.Add(new Path {Data=Geometry.Parse("M 5,9 L 19,2 19,13 13,16 13,27 5,31 Z"),Fill=UI.Brush("#5EEAD4")});
            canvas.Children.Add(new Path {Data=Geometry.Parse("M 21,9 L 35,2 35,27 21,35 21,24 27,21 27,13 21,16 Z"),Fill=UI.Brush("#A99CFF")});
            return new Viewbox {Width=size,Height=size,Child=canvas};
        }
    }
    public sealed class OrbView : Window {
        public static readonly int[] Scales={80,100,125,150,200};
        readonly MonitorApp app;
        readonly Path shortArc,longArc;
        public readonly TextBlock ShortText,LongText;
        readonly TextBlock state;
        public bool Exiting;
        public OrbView(MonitorApp app) {
            this.app=app;Title="Codex 额度 · Token Monitor";
            WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
            ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;
            var canvas=new Canvas {Width=100,Height=100,Background=Brushes.Transparent};
            var disc=new Ellipse {Width=98,Height=98,Fill=UI.Brush("#171D2B"),Stroke=UI.Brush("#414B61"),StrokeThickness=1};Canvas.SetLeft(disc,1);Canvas.SetTop(disc,1);canvas.Children.Add(disc);
            AddArc(canvas,214,112,"#30434B");AddArc(canvas,340,220,"#38354E");
            shortArc=AddArc(canvas,214,112,"#5EEAD4");longArc=AddArc(canvas,340,220,"#A99CFF");
            ShortText=Label(canvas,16,"5h  —", "#8EF3E1");LongText=Label(canvas,68,"周  —","#C7BEFF");
            var mark=Brand.Mark(29);Canvas.SetLeft(mark,35.5);Canvas.SetTop(mark,37);canvas.Children.Add(mark);
            state=Label(canvas,43,"", "#FFBF75");state.FontSize=13;state.Width=18;Canvas.SetLeft(state,74);
            Content=new Viewbox {Child=canvas};ApplyScale();Loaded+=(s,e)=>Clamp();
            var menu=new ContextMenu();
            AddMenu(menu,"展开额度详情",()=>app.SetOrbMode(false));AddMenu(menu,"打开控制中心",app.ShowControl);
            AddMenu(menu,"立即刷新",async()=>await app.RefreshGpt(true));AddMenu(menu,"设置悬浮球大小",()=>{app.ShowControl();new SettingsView(app){Owner=app.Main}.ShowDialog();});
            AddMenu(menu,"隐藏悬浮球",()=>app.Main.GptSwitch.IsChecked=false);ContextMenu=menu;
            MouseLeftButtonDown+=(s,e)=>{if(e.ClickCount==2){app.SetOrbMode(false);return;}try {DragMove();Clamp();app.Save();}catch(InvalidOperationException){}};
            KeyDown+=(s,e)=>{if(e.Key==Key.Enter)app.SetOrbMode(false);};
            Closing+=(s,e)=>{if(!Exiting){e.Cancel=true;app.Main.GptSwitch.IsChecked=false;}};
            AutomationProperties.SetName(this,"Codex 悬浮球，上方 5 小时、下方每周剩余额度；双击展开详情");
        }
        static void AddMenu(ContextMenu menu,string title,Action action){var item=new MenuItem {Header=title};item.Click+=(s,e)=>action();menu.Items.Add(item);}
        static TextBlock Label(Canvas canvas,double top,string text,string color) {
            var label=new TextBlock {Text=text,Foreground=UI.Brush(color),FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI"),FontWeight=FontWeights.SemiBold,FontSize=14,Width=84,TextAlignment=TextAlignment.Center};
            Canvas.SetLeft(label,8);Canvas.SetTop(label,top);canvas.Children.Add(label);return label;
        }
        static Path AddArc(Canvas canvas,double start,double sweep,string color) {
            var path=new Path {Stroke=UI.Brush(color),StrokeThickness=4,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Data=Arc(start,sweep)};canvas.Children.Add(path);return path;
        }
        public static Geometry Arc(double start,double sweep) {
            if(sweep<=0)return Geometry.Empty;
            Func<double,Point> point=angle=>new Point(50+44*Math.Cos(angle*Math.PI/180),50+44*Math.Sin(angle*Math.PI/180));
            var figure=new PathFigure {StartPoint=point(start),IsClosed=false,IsFilled=false};
            figure.Segments.Add(new ArcSegment(point(start+sweep),new Size(44,44),0,sweep>180,SweepDirection.Clockwise,true));
            return new PathGeometry(new[]{figure});
        }
        public void ApplyScale(){Width=Height=76.0*app.Prefs.OrbScale/100; if(IsLoaded)Clamp();}
        public void Place(){var area=SystemParameters.WorkArea;Left=Finite(app.Prefs.OrbX)?app.Prefs.OrbX:area.Right-Width-24;Top=Finite(app.Prefs.OrbY)?app.Prefs.OrbY:area.Top+50;Clamp();}
        static bool Finite(double n){return n!=-1&&!double.IsNaN(n)&&!double.IsInfinity(n);}
        public void Clamp(){
            // Resolve the nearest physical monitor, then convert its work area back to WPF units.
            var dpi=PresentationSource.FromVisual(this);var matrix=dpi==null?Matrix.Identity:dpi.CompositionTarget.TransformToDevice;
            var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)((Left+Width/2)*matrix.M11),(int)((Top+Height/2)*matrix.M22))).WorkingArea;
            Left=Math.Max(screen.Left/matrix.M11,Math.Min(Left,screen.Right/matrix.M11-Width));Top=Math.Max(screen.Top/matrix.M22,Math.Min(Top,screen.Bottom/matrix.M22-Height));
        }
        public void Render(GptSnapshot data,string error,bool busy) {
            var group=data==null?null:data.Groups.FirstOrDefault(x=>x.Id=="codex");
            var shortWindow=group==null?null:group.Windows.FirstOrDefault(x=>x.Label=="5 小时额度");
            var week=group==null?null:group.Windows.FirstOrDefault(x=>x.Label=="每周额度");
            Draw(shortArc,ShortText,shortWindow,214,112,"5h");Draw(longArc,LongText,week,340,220,"周");
            state.Text=error!=null?"!":busy?"·":"";
            shortArc.Opacity=longArc.Opacity=error==null?1:0.35;
            ToolTip="Codex · 剩余额度\n"+ShortText.Text+" / "+LongText.Text+(error!=null?"\n更新失败（显示上次数据）："+error:busy?"\n正在刷新…":"")+(data==null?"":"\n上次同步 "+UI.Clock(data.Fetched))+"\n双击展开详情 · 右键菜单";
            AutomationProperties.SetName(this,ToolTip.ToString());
        }
        static void Draw(Path arc,TextBlock text,QuotaWindow window,double start,double sweep,string label){
            double? remaining=window==null?null:window.Remaining;
            arc.Data=Arc(start,remaining.HasValue?sweep*Math.Max(0,Math.Min(100,remaining.Value))/100:0);
            text.Text=label+"  "+(remaining.HasValue?remaining.Value.ToString("0")+"%":"—");
        }
    }
}
