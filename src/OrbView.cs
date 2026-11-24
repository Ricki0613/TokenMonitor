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
        public const string Mint="#5EEAD4", Violet="#A99CFF", Navy="#171D2B";
        public static FrameworkElement Mark(double size) {
            var canvas=new Canvas {Width=40,Height=40};
            canvas.Children.Add(new Path {Data=Geometry.Parse("M 5,9 L 19,2 19,13 13,16 13,27 5,31 Z"),Fill=UI.Brush(Mint)});
            canvas.Children.Add(new Path {Data=Geometry.Parse("M 21,9 L 35,2 35,27 21,35 21,24 27,21 27,13 21,16 Z"),Fill=UI.Brush(Violet)});
            return new Viewbox {Width=size,Height=size,Child=canvas};
        }
    }
    public sealed class OrbView : Window {
        public static readonly int[] Scales={80,100,125,150,200};
        readonly MonitorApp app;
        readonly bool kimi;
        readonly Path shortArc,longArc;
        public readonly TextBlock ShortText,LongText;
        readonly TextBlock state;
        internal readonly TextBlock HoverShort,HoverWeek,HoverHint;
        readonly ToolTip hoverTip;
        public bool Exiting;
        public OrbView(MonitorApp app,bool kimi=false) {
            this.app=app;this.kimi=kimi;Title=(kimi?"Kimi":"ChatGPT")+" 额度 · Token Monitor";
            WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;
            ResizeMode=ResizeMode.NoResize;Topmost=true;ShowInTaskbar=false;
            var canvas=new Canvas {Width=100,Height=100,Background=Brushes.Transparent};
            var disc=new Ellipse {Width=98,Height=98,Fill=UI.Brush("#171D2B"),Stroke=UI.Brush("#414B61"),StrokeThickness=1};Canvas.SetLeft(disc,1);Canvas.SetTop(disc,1);canvas.Children.Add(disc);
            disc.SetResourceReference(Shape.FillProperty,"SurfaceBrush");
            disc.SetResourceReference(Shape.StrokeProperty,"BorderBrush");
            AddArc(canvas,kimi?270:214,kimi?359.99:112,"TrackBrush");
            if(!kimi)AddArc(canvas,340,220,"TrackBrush");
            shortArc=AddArc(canvas,kimi?270:214,kimi?0:112,"GreenBrush");
            longArc=AddArc(canvas,340,kimi?0:220,"BlueBrush");
            ShortText=Label(canvas,kimi?51:16,"—","InkBrush");
            LongText=Label(canvas,68,"—","InkBrush");
            if(kimi){LongText.Visibility=Visibility.Collapsed;Label(canvas,30,"Kimi","MutedBrush");}
            else {var mark=Brand.Mark(29);Canvas.SetLeft(mark,35.5);Canvas.SetTop(mark,37);canvas.Children.Add(mark);}
            state=Label(canvas,43,"","WarningBrush");state.FontSize=13;state.Width=18;Canvas.SetLeft(state,74);
            Content=new Viewbox {Child=canvas};ApplyScale();Loaded+=(s,e)=>Clamp();
            var tooltipBody=new StackPanel();
            var resetRows=new Grid();resetRows.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});resetRows.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
            var times=new StackPanel();Grid.SetColumn(times,1);resetRows.Children.Add(times);
            HoverShort=UI.Text("待同步  (5h)",14,UI.Ink);HoverShort.Margin=new Thickness(0,0,0,4);times.Children.Add(HoverShort);
            HoverWeek=UI.Text("待同步  (week)",14,UI.Ink);times.Children.Add(HoverWeek);if(kimi)HoverWeek.Visibility=Visibility.Collapsed;
            tooltipBody.Children.Add(resetRows);
            HoverHint=UI.Text("双击详情 · 右键菜单",11.5,UI.Muted);HoverHint.Margin=new Thickness(0,10,0,0);tooltipBody.Children.Add(HoverHint);
            hoverTip=new ToolTip {Content=tooltipBody,Padding=new Thickness(15,12,15,11)};
            hoverTip.SetResourceReference(Control.BackgroundProperty,"CardBrush");hoverTip.SetResourceReference(Control.BorderBrushProperty,"BorderBrush");
            var template=new ControlTemplate(typeof(ToolTip));var surface=new FrameworkElementFactory(typeof(Border));
            surface.SetValue(Border.CornerRadiusProperty,new CornerRadius(10));surface.SetValue(Border.BorderThicknessProperty,new Thickness(1));
            surface.SetValue(Border.BackgroundProperty,new TemplateBindingExtension(Control.BackgroundProperty));
            surface.SetValue(Border.BorderBrushProperty,new TemplateBindingExtension(Control.BorderBrushProperty));
            surface.SetValue(Border.PaddingProperty,new TemplateBindingExtension(Control.PaddingProperty));
            surface.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));template.VisualTree=surface;hoverTip.Template=template;
            ToolTip=hoverTip;ToolTipService.SetShowDuration(this,20000);
            var menu=new ContextMenu();
            AddMenu(menu,"切换为悬浮窗",ShowDetails);AddMenu(menu,"控制中心",app.ShowControl);
            AddMenu(menu,"立即刷新",async()=>{if(kimi)await app.UpdateKUsage();else await app.RefreshGpt(true);});
            AddMenu(menu,"隐藏",HideService);ContextMenu=menu;
            PreviewMouseDoubleClick+=(s,e)=>{if(e.ChangedButton==MouseButton.Left){e.Handled=true;ShowDetails();}};
            MouseLeftButtonDown+=(s,e)=>{if(e.Handled)return;try {DragMove();Clamp();app.Save();}catch(InvalidOperationException){}};
            KeyDown+=(s,e)=>{if(e.Key==Key.Enter)ShowDetails();};
            Closing+=(s,e)=>{if(!Exiting){e.Cancel=true;HideService();}};
            AutomationProperties.SetName(this,kimi?"Kimi 订阅剩余额度悬浮球":"ChatGPT 悬浮球，上方 5 小时、下方每周剩余额度；双击展开详情");
        }
        void ShowDetails(){if(kimi)app.SetKimiOrbMode(false);else app.SetOrbMode(false);}
        void HideService(){if(kimi)app.SetKimiVisible(false,false);else app.Main.GptSwitch.IsChecked=false;}
        internal void RenderKimi(QuotaWindow window,string error){
            Draw(shortArc,ShortText,window,270,359.99);
            state.Text=error==null?"":"!";
            HoverShort.Text="重置 "+ResetTime(window);
            HoverHint.Text=error??"双击详情 · 右键菜单";
        }
        static void AddMenu(ContextMenu menu,string title,Action action){var item=new MenuItem {Header=title};item.Click+=(s,e)=>action();menu.Items.Add(item);}
        static TextBlock Label(Canvas canvas,double top,string text,string color) {
            var label=new TextBlock {Text=text,FontFamily=(FontFamily)Application.Current.FindResource("InterfaceFont"),FontWeight=FontWeights.SemiBold,FontSize=14,Width=84,TextAlignment=TextAlignment.Center};
            label.SetResourceReference(TextBlock.ForegroundProperty,color);Canvas.SetLeft(label,8);Canvas.SetTop(label,top);canvas.Children.Add(label);return label;
        }
        static Path AddArc(Canvas canvas,double start,double sweep,string color) {
            var path=new Path {StrokeThickness=4,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,Data=Arc(start,sweep)};path.SetResourceReference(Shape.StrokeProperty,color);canvas.Children.Add(path);return path;
        }
        public static Geometry Arc(double start,double sweep) {
            if(sweep<=0)return Geometry.Empty;
            Func<double,Point> point=angle=>new Point(50+44*Math.Cos(angle*Math.PI/180),50+44*Math.Sin(angle*Math.PI/180));
            var figure=new PathFigure {StartPoint=point(start),IsClosed=false,IsFilled=false};
            figure.Segments.Add(new ArcSegment(point(start+sweep),new Size(44,44),0,sweep>180,SweepDirection.Clockwise,true));
            return new PathGeometry(new[]{figure});
        }
        public void ApplyScale(){Width=Height=76.0*app.Prefs.OrbScale/100; if(IsLoaded)Clamp();}
        public void Place(){
            var area=SystemParameters.WorkArea;
            double x=kimi?app.Prefs.KimiOrbX:app.Prefs.OrbX,y=kimi?app.Prefs.KimiOrbY:app.Prefs.OrbY;
            Left=Finite(x)?x:area.Right-Width-24;Top=Finite(y)?y:area.Top+50+(kimi?Width+10:0);
            Clamp();
        }
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
            Draw(shortArc,ShortText,shortWindow,214,112);Draw(longArc,LongText,week,340,220);
            state.Text=error!=null?"!":busy?"·":"";
            shortArc.Opacity=longArc.Opacity=error==null?1:0.35;
            string shortReset=ResetTime(shortWindow),weekReset=ResetTime(week);
            HoverShort.Text=shortReset+"  (5h)";HoverWeek.Text=weekReset+"  (week)";
            bool stale=error!=null&&data!=null;
            HoverHint.Text="双击详情 · 右键菜单"+(stale?" · 旧数据":"");
            string accessible="重置："+HoverShort.Text+"\n　　　"+HoverWeek.Text+"\n"+HoverHint.Text+
                (error!=null?"\n更新失败"+(stale?"（显示上次数据）":"")+"："+error:busy?"\n正在刷新…":"");
            AutomationProperties.SetName(hoverTip,accessible);AutomationProperties.SetName(this,accessible);
        }
        static string ResetTime(QuotaWindow window) {
            return window!=null&&window.Reset.HasValue?UI.China(window.Reset.Value).ToString("yyyy.MM.dd HH:mm",System.Globalization.CultureInfo.InvariantCulture):"待同步";
        }
        static void Draw(Path arc,TextBlock text,QuotaWindow window,double start,double sweep){
            double? remaining=window==null?null:window.Remaining;
            arc.Data=Arc(start,remaining.HasValue?sweep*Math.Max(0,Math.Min(100,remaining.Value))/100:0);
            text.Text=remaining.HasValue?remaining.Value.ToString("0")+"%":"—";
        }
    }
}
