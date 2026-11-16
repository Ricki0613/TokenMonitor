using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokenMonitor {
    public sealed class DashboardCard : Border {
        readonly StackPanel metrics=new StackPanel();
        object last;
        bool rendered;
        readonly bool compact;
        bool focus;
        readonly List<TextBlock> values=new List<TextBlock>();
        readonly List<Grid> tracks=new List<Grid>();
        internal void SetFocusLayout(bool enabled) {
            focus=enabled;
            foreach(var value in values)FontSizing.Bind(value,TextBlock.FontSizeProperty,focus?21:13);
            foreach(var track in tracks)StyleTrack(track);
        }
        void ClearMetrics(){metrics.Children.Clear();values.Clear();tracks.Clear();}
        TextBlock Value(string value) {
            var text=UI.Field(value);
            values.Add(text);
            if(focus)FontSizing.Bind(text,TextBlock.FontSizeProperty,21);
            return text;
        }
        void StyleTrack(Grid track) {
            track.Height=focus?9:4;
            track.Margin=focus?new Thickness(0,8,0,8):new Thickness(0,3,0,3);
        }
        public DashboardCard(bool compact=false) {this.compact=compact;Child=metrics;}
        public void Render(object snapshot,int service) {
            if(rendered&&ReferenceEquals(last,snapshot))return;last=snapshot;rendered=true;ClearMetrics();
            var gpt=snapshot as GptSnapshot;
            if(gpt!=null) {
                var group=gpt.Groups.FirstOrDefault(x=>x.Id=="codex");
                if(group!=null)foreach(var w in group.Windows.Take(2)) {
                    AddQuota(w);
                }
                if(metrics.Children.Count>0)return;
            }
            var deep=snapshot as DeepSnapshot;
            if(deep!=null){
                foreach(var b in deep.Balances){
                    AddMetric("可用余额 · "+b.Currency,b.Format(b.Total));
                    if(!compact)AddBreakdown("充值余额",b.Format(b.ToppedUp),"赠送余额",b.Format(b.Granted));
                }
                return;
            }
            var open=snapshot as OpenAiSnapshot;
            if(open!=null){AddMetric("今日 · tokens",open.Today.TotalTokens.ToString("N0"));AddMetric("本月 · tokens",open.Month.TotalTokens.ToString("N0"));return;}
            AddMetric(service==0?"剩余额度":service==1?"可用余额":"今日 / 本月 tokens","—");
            if(!compact)metrics.Children.Add(UI.Text("连接后在这里查看"+(service==0?"两个周期的剩余额度":service==1?"账户余额":"历史用量"),12,UI.Muted));
        }
        internal void RenderQuota(QuotaWindow window) {
            ClearMetrics();
            AddQuota(window);
        }
        internal void RenderBalance(double? amount,double? cash=null,double? voucher=null) {
            ClearMetrics();
            AddMetric("可用余额 · CNY",amount.HasValue?"¥"+amount.Value.ToString("0.00"):"—");
            if(!compact&&(cash.HasValue||voucher.HasValue))AddBreakdown("现金余额",cash.HasValue?"¥"+cash.Value.ToString("0.00"):null,"代金券余额",voucher.HasValue?"¥"+voucher.Value.ToString("0.00"):null);
        }
        void AddBreakdown(string first,string firstValue,string second,string secondValue) {
            var group=new StackPanel {Margin=new Thickness(0,2,0,8)};
            if(firstValue!=null)group.Children.Add(UI.Row(UI.Hint(first),UI.Hint(firstValue)));
            if(secondValue!=null)group.Children.Add(UI.Row(UI.Hint(second),UI.Hint(secondValue)));
            metrics.Children.Add(group);
        }
        void AddQuota(QuotaWindow w) {
            var block=new StackPanel {Margin=new Thickness(0,0,0,5)};
            block.Children.Add(UI.Row(UI.Hint(w.Label),Value(w.Remaining.HasValue?w.Remaining.Value.ToString("0.##")+"%":"—")));
            var track=new Grid();tracks.Add(track);StyleTrack(track);
            var background=new Border {CornerRadius=new CornerRadius(3)};background.SetResourceReference(BackgroundProperty,"TrackBrush");track.Children.Add(background);
            var bar=new Border {CornerRadius=new CornerRadius(3),HorizontalAlignment=HorizontalAlignment.Left};bar.SetResourceReference(BackgroundProperty,"GreenBrush");
            double remaining=Math.Max(0,Math.Min(100,w.Remaining.GetValueOrDefault()));track.SizeChanged+=(sender,args)=>bar.Width=track.ActualWidth*remaining/100;track.Children.Add(bar);block.Children.Add(track);
            block.Children.Add(UI.Hint(w.Reset.HasValue?"重置 "+UI.China(w.Reset.Value).ToString("MM-dd HH:mm")+(compact?"":" · 北京时间"):"重置时间待同步"));metrics.Children.Add(block);
        }
        void AddMetric(string label,string value) {
            var caption=UI.Hint(label);
            caption.VerticalAlignment=VerticalAlignment.Center;
            var row=UI.Row(caption,Value(value));
            row.Margin=new Thickness(0,0,0,5);
            metrics.Children.Add(row);
        }
    }
}
