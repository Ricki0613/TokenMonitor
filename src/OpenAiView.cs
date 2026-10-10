using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace TokenMonitor {
    public sealed class OpenAiView : Shell {
        readonly MonitorApp app;
        readonly StackPanel details=new StackPanel();
        readonly TextBlock status=UI.Text("等待连接",10.5,UI.Muted);
        readonly TextBlock interval=UI.Text("",10,UI.Muted);
        public readonly Button Refresh;
        public OpenAiView(MonitorApp owner):base("OpenAI","API Key 用量 · 北京时间 UTC+8",UI.Green,368,500,true) {
            app=owner;Body.Children.Add(details);
            var buttons=new WrapPanel {Margin=new Thickness(0,0,0,-6)};
            Refresh=UI.Button("刷新",async()=>await app.RefreshOpenAi(true));Refresh.Margin=new Thickness(0,0,6,6);buttons.Children.Add(Refresh);
            var official=UI.Button("官方用量 ↗",()=>UI.Link("https://platform.openai.com/usage"));official.Margin=new Thickness(0,0,6,6);buttons.Children.Add(official);
            var settings=UI.Button("设置",()=>new SettingsView(app){Owner=this}.ShowDialog(),"连接 OpenAI API Key");settings.Margin=new Thickness(0,0,6,6);buttons.Children.Add(settings);
            Body.Children.Add(buttons);status.Margin=new Thickness(1,13,0,0);Body.Children.Add(status);
            interval.Margin=new Thickness(1,5,0,0);Body.Children.Add(interval);
            CloseAction=()=>app.Main.OpenAiSwitch.IsChecked=false;
            Render(null,null,false);
        }
        public void Render(OpenAiSnapshot snapshot,string error,bool loading) {
            details.Children.Clear();
            if(snapshot!=null) {
                var cards=new CardPanel {Margin=new Thickness(0,0,0,10)};
                cards.Children.Add(UsageCard("今日",UI.China(snapshot.End).ToString("MM-dd",CultureInfo.InvariantCulture),snapshot.Today));
                cards.Children.Add(UsageCard("本月",UI.China(snapshot.End).ToString("yyyy-MM",CultureInfo.InvariantCulture),snapshot.Month));
                details.Children.Add(cards);
                var target=UI.Text("API Key ID · "+snapshot.ApiKeyId,10.5,UI.Muted);target.Margin=new Thickness(2,0,0,8);target.ToolTip=snapshot.ApiKeyId;details.Children.Add(target);
                var note=UI.Text("缓存输入已计入输入 token。官方用量可能延迟更新。",10.5,UI.Muted);note.Margin=new Thickness(2,0,0,14);details.Children.Add(note);
            } else {
                var empty=new StackPanel {Margin=new Thickness(0,10,0,12)};
                empty.Children.Add(UI.Text(loading?"正在同步":"等待用量数据",20,UI.Ink,true));
                var desc=UI.Text(loading?"读取此 API Key 的今日与本月 token 用量。":error??"在设置中使用 OpenAI API Key 登录，并配置 Admin API Key 与对应的 API Key ID。",12,UI.Muted);
                desc.Margin=new Thickness(0,14,0,0);empty.Children.Add(desc);details.Children.Add(UI.Card(empty));
            }
            details.Opacity=error!=null&&snapshot!=null?0.58:1;
            UpdateStatus(snapshot,error,loading);
        }
        static Border UsageCard(string title,string date,OpenAiTokenTotals totals) {
            var box=new StackPanel();box.Children.Add(UI.Row(UI.Text(title,13,UI.Ink,true),UI.Text(date,10.5,UI.Muted)));
            var total=UI.Text(totals.TotalTokens.ToString("N0",CultureInfo.InvariantCulture),32,UI.Ink,true);total.Margin=new Thickness(0,10,0,2);box.Children.Add(total);
            var label=UI.Text("总 token",10.5,UI.Muted);label.Margin=new Thickness(0,0,0,12);box.Children.Add(label);
            var divider=new Border {Height=1,Margin=new Thickness(0,0,0,12)};Appearance.Paint(divider,Border.BackgroundProperty,"#EEF0F3");box.Children.Add(divider);
            AddCount(box,"输入 token",totals.InputTokens,false);AddCount(box,"输出 token",totals.OutputTokens,true);
            AddCount(box,"其中缓存输入",totals.CachedInputTokens,true);AddCount(box,"请求数",totals.Requests,true);
            var card=UI.Card(box,12);card.Margin=new Thickness(0);return card;
        }
        static void AddCount(StackPanel box,string label,long? value,bool gap) {
            var row=UI.Row(UI.Text(label,11.5,UI.Muted),UI.Text(value.HasValue?value.Value.ToString("N0",CultureInfo.InvariantCulture):"—",12,UI.Ink,true));
            if(gap)row.Margin=new Thickness(0,8,0,0);box.Children.Add(row);
        }
        public void UpdateStatus(OpenAiSnapshot snapshot,string error,bool loading) {
            Appearance.Paint(status,TextBlock.ForegroundProperty,error!=null?"#B77436":UI.Muted);
            status.Text=error!=null?(snapshot!=null?"旧数据 · ":"")+error:loading?"正在刷新…":snapshot!=null?"已同步 "+UI.Clock(snapshot.Fetched):"等待同步";
            interval.Text=error!=null&&snapshot!=null?"上次成功 "+UI.China(snapshot.Fetched).ToString("MM-dd HH:mm:ss",CultureInfo.InvariantCulture)+" · 北京时间":"每 "+app.Prefs.Interval+" 秒同步 · 北京时间";
            Refresh.IsEnabled=!loading;
        }
    }
}
