using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokenMonitor {
    // A service card keeps its actions on the same baseline as its neighbours.
    public sealed class DashboardCard : Border {
        readonly StackPanel metrics=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
        object last;
        bool rendered;
        public DashboardCard(string name,string tag,TextBlock status,CheckBox toggle,Action connect,Action compact=null) {
            CornerRadius=new CornerRadius(14);Padding=new Thickness(20);MinHeight=330;
            SetResourceReference(BackgroundProperty,"CardBrush");
            var grid=new Grid();Child=grid;
            grid.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition {Height=GridLength.Auto});
            var title=new StackPanel();title.Children.Add(UI.Text(name,19,UI.Ink,true));
            title.Children.Add(UI.Text(tag,11,UI.Muted));grid.Children.Add(title);
            Grid.SetRow(metrics,1);metrics.Margin=new Thickness(0,22,0,22);grid.Children.Add(metrics);
            var footer=new StackPanel();Grid.SetRow(footer,2);grid.Children.Add(footer);
            status.Margin=new Thickness(0,0,0,14);status.MinHeight=32;footer.Children.Add(status);
            var actions=new WrapPanel();var button=UI.Button("连接设置",connect);button.Margin=new Thickness(0,0,8,10);actions.Children.Add(button);
            if(compact!=null){var orb=UI.Button("悬浮球",compact);orb.Margin=new Thickness(0,0,0,10);actions.Children.Add(orb);}
            footer.Children.Add(actions);footer.Children.Add(UI.Row(UI.Text("显示浮窗",12,UI.Ink),toggle));
            toggle.ToolTip="只控制桌面浮窗；已连接服务继续在概览刷新。";
        }
        public void Render(object snapshot,int service) {
            if(rendered&&ReferenceEquals(last,snapshot))return;last=snapshot;rendered=true;metrics.Children.Clear();
            var gpt=snapshot as GptSnapshot;
            if(gpt!=null) {
                var group=gpt.Groups.FirstOrDefault(x=>x.Id=="codex");
                if(group!=null)foreach(var w in group.Windows.Take(2)) {
                    var block=new StackPanel {Margin=new Thickness(0,0,0,18)};
                    block.Children.Add(UI.Row(UI.Text(w.Label,12,UI.Muted),UI.Text(w.Remaining.HasValue?w.Remaining.Value.ToString("0.#")+"%":"—",28,UI.Ink,true)));
                    var track=new Grid {Height=6,Margin=new Thickness(0,9,0,8)};
                    var background=new Border {CornerRadius=new CornerRadius(3)};background.SetResourceReference(BackgroundProperty,"SurfaceBrush");track.Children.Add(background);
                    var bar=new Border {CornerRadius=new CornerRadius(3),HorizontalAlignment=HorizontalAlignment.Left};bar.SetResourceReference(BackgroundProperty,"GreenBrush");
                    double remaining=Math.Max(0,Math.Min(100,w.Remaining.GetValueOrDefault()));track.SizeChanged+=(sender,args)=>bar.Width=track.ActualWidth*remaining/100;track.Children.Add(bar);block.Children.Add(track);
                    block.Children.Add(UI.Text(w.Reset.HasValue?"重置 "+UI.China(w.Reset.Value).ToString("MM-dd HH:mm")+" · 北京时间":"重置时间待同步",10.5,UI.Muted));metrics.Children.Add(block);
                }
                if(metrics.Children.Count>0)return;
            }
            var deep=snapshot as DeepSnapshot;
            if(deep!=null){foreach(var b in deep.Balances)AddMetric("可用余额 · "+b.Currency,b.Format(b.Total));return;}
            var open=snapshot as OpenAiSnapshot;
            if(open!=null){AddMetric("今日 · tokens",open.Today.TotalTokens.ToString("N0"));AddMetric("本月 · tokens",open.Month.TotalTokens.ToString("N0"));return;}
            AddMetric(service==0?"剩余额度":service==1?"可用余额":"今日 / 本月 tokens","—");
            metrics.Children.Add(UI.Text("连接后在这里查看"+(service==0?"两个周期的剩余额度":service==1?"账户余额":"历史用量"),12,UI.Muted));
        }
        void AddMetric(string label,string value){metrics.Children.Add(UI.Text(label,12,UI.Muted));var text=UI.Text(value,30,UI.Ink,true);text.Margin=new Thickness(0,5,0,20);metrics.Children.Add(text);}
    }
}
