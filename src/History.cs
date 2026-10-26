using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path=System.IO.Path;

namespace TokenMonitor {
    public sealed class HistoryPoint {
        public string Series, Day;
        public long Time;
        public decimal Value;
    }
    public sealed class HistoryStore {
        public readonly List<HistoryPoint> Points;
        readonly string path;
        bool dirty;
        public HistoryStore(string path) {
            this.path=path;
            Points=File.Exists(path)?new JavaScriptSerializer {MaxJsonLength=16777216}.Deserialize<List<HistoryPoint>>(File.ReadAllText(path)):new List<HistoryPoint>();
            if(Points==null||Points.Any(p=>p==null||string.IsNullOrEmpty(p.Series)))throw new InvalidDataException("历史记录格式无效；原文件已保留。");
        }
        public static string Key(string value) {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value??""))).Replace("-","").Substring(0,12);}
        public void Put(string series,DateTimeOffset at,decimal value,string day=null) {
            long time=at.ToUnixTimeSeconds();
            // Keep the most recent successful observation in each 15-minute bucket.
            var old=Points.FirstOrDefault(p=>p.Series==series&&(day!=null?p.Day==day:p.Day==null&&p.Time/900==time/900));
            if(old!=null){if(old.Time>time||old.Value==value)return;old.Time=time;old.Value=value;}
            else Points.Add(new HistoryPoint {Series=series,Time=time,Value=value,Day=day});
            dirty=true;
        }
        public void Save(DateTimeOffset now) {
            long cutoff=now.AddDays(-90).ToUnixTimeSeconds();
            int removed=Points.RemoveAll(p=>p.Time<cutoff);
            if(!dirty&&removed==0&&File.Exists(path))return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Atomic.Write(path,Encoding.UTF8.GetBytes(new JavaScriptSerializer {MaxJsonLength=16777216}.Serialize(Points)));dirty=false;
        }
        public void Record(object data) {
            var g=data as GptSnapshot;
            if(g!=null)foreach(var group in g.Groups.Where(x=>x.Id=="codex"))foreach(var w in group.Windows)
                if(w.Remaining.HasValue)Put("ChatGPT · "+w.Label+" · %",g.Fetched,(decimal)w.Remaining.Value);
            var d=data as DeepSnapshot;
            if(d!=null)foreach(var b in d.Balances)Put("DeepSeek · "+(d.FromHarness?"Harness":"API")+" · "+b.Currency,d.Fetched,b.Total);
            var o=data as OpenAiSnapshot;
            if(o!=null)foreach(var day in o.Daily)Put("OpenAI · "+Key(o.ApiKeyId)+" · tokens",o.Fetched,day.Value.TotalTokens,day.Key);
            Save(DateTimeOffset.UtcNow);
        }
    }
    public sealed partial class MonitorApp {
        public string HistoryNotice;
        HistoryStore historyStore;string historyPath;
        void RecordHistory(object data) {
            if(smoke||offline)return;
            try {string path=Path.Combine(Paths.Data,"history.json");if(historyStore==null||historyPath!=path){historyStore=new HistoryStore(path);historyPath=path;}historyStore.Record(data);HistoryNotice=null;if(Main!=null)Main.HistoryChanged();}
            catch {HistoryNotice="历史记录暂时无法保存，请检查数据目录权限和 history.json；实时查询不受影响。";}
        }
    }
    public sealed class HistoryView : UserControl {
        readonly StackPanel charts=new StackPanel();
        readonly MonitorApp app;
        int days=7;
        public HistoryView(MonitorApp app) {
            this.app=app;var root=new DockPanel {Margin=new Thickness(18,4,18,18)};Content=root;
            var header=new StackPanel {Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
            header.Children.Add(UI.Text("历史趋势",24,UI.Ink,true));
            header.Children.Add(UI.Text("本机记录 · 北京时间 · 保留 90 天 · 额度与余额每 15 分钟保留最新采样",12,UI.Muted));
            var choices=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,8)};
            foreach(int count in new[]{7,30}){int n=count;var b=UI.Button("近 "+n+" 天",()=>{days=n;Render();});b.Margin=new Thickness(0,0,8,0);choices.Children.Add(b);}header.Children.Add(choices);
            header.Children.Add(UI.Text("余额变化不等于消费：充值、赠送和退款都会影响余额。ChatGPT / DeepSeek 为本机来源记录，切换登录账号也会影响曲线。",12,UI.Muted));
            root.Children.Add(new ScrollViewer {Content=charts,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Render();
        }
        public void Render() {
            charts.Children.Clear();
            try {
                var store=new HistoryStore(Path.Combine(Paths.Data,"history.json"));
                var start=OpenAiSnapshot.DayStart(DateTimeOffset.UtcNow).AddDays(1-days);var end=start.AddDays(days);
                foreach(var series in store.Points.GroupBy(p=>p.Series).OrderBy(p=>p.Key)) {
                    var points=series.Where(p=>Time(p)>=start&&Time(p)<end).OrderBy(Time).ToList();if(points.Count==0)continue;
                    var panel=new StackPanel();panel.Children.Add(UI.Text(series.Key+(series.Key.StartsWith("DeepSeek")?" · 余额变化":""),16,UI.Ink,true));
                    string detail=points[0].Day==null?"最新 "+points.Last().Value.ToString("0.##")+(points.Count>1?" · 区间变化 "+(points.Last().Value-points[0].Value).ToString("+0.##;-0.##;0"):" · 等待更多采样"):
                        "所选区间已记录 "+points.Sum(p=>p.Value).ToString("N0")+" tokens · 当天数据未结算";
                    panel.Children.Add(UI.Text(detail,12,UI.Muted));
                    var canvas=new Canvas {Height=150,Margin=new Thickness(48,12,18,0),ClipToBounds=false};panel.Children.Add(canvas);
                    Action draw=()=>Draw(canvas,points,start,end);canvas.SizeChanged+=(s,e)=>draw();canvas.Loaded+=(s,e)=>draw();
                    panel.Children.Add(UI.Row(UI.Text(UI.China(start).ToString("MM-dd"),11,UI.Muted),UI.Text(UI.China(end.AddDays(-1)).ToString("MM-dd"),11,UI.Muted)));
                    charts.Children.Add(UI.Card(panel));
                }
                if(charts.Children.Count==0)charts.Children.Add(UI.Card(UI.Text("近 "+days+" 天暂无记录。连接服务并成功刷新后开始记录；不会补造离线期间的数据。",15,UI.Muted)));
                charts.Children.Add(UI.Text(app.HistoryNotice??"OpenAI 按 API Key ID 分开记录，每次刷新修正已获取的本月每日 tokens；以前月份仅保留已采集数据。缺失日期不按零计算。",12,UI.Muted));
            }catch{charts.Children.Add(UI.Text("无法读取历史记录。原文件保留，请检查数据目录中的 history.json。",14,UI.Muted));}
        }
        static DateTimeOffset Time(HistoryPoint p) {return p.Day==null?DateTimeOffset.FromUnixTimeSeconds(p.Time):DateTimeOffset.ParseExact(p.Day+" +08:00","yyyy-MM-dd zzz",CultureInfo.InvariantCulture);}
        static void Draw(Canvas canvas,List<HistoryPoint> points,DateTimeOffset start,DateTimeOffset end) {
            canvas.Children.Clear();double width=canvas.ActualWidth;if(width<=0)return;
            double min=(double)points.Min(p=>p.Value),max=(double)points.Max(p=>p.Value);
            bool daily=points[0].Day!=null;
            if(daily)min=0;if(points[0].Series.StartsWith("ChatGPT")){min=0;max=100;}if(max<=min)max=min+1;
            for(int i=0;i<3;i++) {double y=10+i*60;var line=new Line {X1=0,X2=width,Y1=y,Y2=y,StrokeThickness=1};line.SetResourceReference(Shape.StrokeProperty,"BorderBrush");canvas.Children.Add(line);var label=UI.Text((max-(max-min)*i/2).ToString("0.#"),10,UI.Muted);Canvas.SetLeft(label,-48);Canvas.SetTop(label,y-7);canvas.Children.Add(label);}
            Point? prev=null;DateTimeOffset previous=start;
            foreach(var p in points) {
                var at=Time(p);double x=(at-start).TotalSeconds/(end-start).TotalSeconds*width,y=130-((double)p.Value-min)/(max-min)*120;
                string tip=UI.China(at).ToString(p.Day==null?"MM-dd HH:mm":"yyyy-MM-dd")+" · "+p.Value.ToString("0.##");
                if(daily){var bar=new Rectangle {Width=Math.Max(2,width/(end-start).TotalDays-5),Height=Math.Max(1,130-y),ToolTip=tip};bar.SetResourceReference(Shape.FillProperty,"GreenBrush");Canvas.SetLeft(bar,x+2);Canvas.SetTop(bar,y);canvas.Children.Add(bar);}
                else {if(prev.HasValue&&(at-previous).TotalHours<=24){var line=new Line {X1=prev.Value.X,Y1=prev.Value.Y,X2=x,Y2=y,StrokeThickness=2,ToolTip=tip};line.SetResourceReference(Shape.StrokeProperty,"GreenBrush");canvas.Children.Add(line);}var dot=new Ellipse {Width=5,Height=5,ToolTip=tip};dot.SetResourceReference(Shape.FillProperty,"GreenBrush");Canvas.SetLeft(dot,x-2.5);Canvas.SetTop(dot,y-2.5);canvas.Children.Add(dot);}
                prev=new Point(x,y);previous=at;
            }
        }
    }
}

