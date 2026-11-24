using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace TokenMonitor {
    public sealed class CompactView : Shell {
        readonly DashboardCard metrics=new DashboardCard(true);
        readonly Button refresh;
        readonly int service;
        bool pending;
        readonly TextBlock warning=UI.Text("⚠ 额度不足",11,"#B77436",true);
        public Action SwitchDisplay;
        readonly Func<Task> sync;
        public CompactView(string title,int service,Func<Task> sync,Action hide)
            :base(title,"",UI.Green,230,100,true) {
            this.service=service;this.sync=sync;
            PreviewMouseDoubleClick+=(s,e)=>{
                if(e.ChangedButton!=MouseButton.Left||SwitchDisplay==null)return;
                var node=e.OriginalSource as DependencyObject;
                while(node!=null&&node!=this){if(node is ButtonBase)return;node=VisualTreeHelper.GetParent(node);}
                e.Handled=true;SwitchDisplay();
            };
            CloseAction=hide;
            refresh=UI.Button("↻",async()=>{
                if(pending)return;
                pending=true;refresh.IsEnabled=false;
                try {await sync();} finally {pending=false;refresh.IsEnabled=true;}
            },"刷新");
            refresh.Width=25;refresh.Height=23;refresh.Padding=new Thickness(0);
            refresh.Background=Brushes.Transparent;
            var arrow=new System.Windows.Shapes.Path {
                Width=14,Height=14,StrokeThickness=1.4,
                Data=Geometry.Parse("M11.8,5.5 A4.8,4.8 0 1 0 12,8 M12,2.5 L12,5.8 L8.7,5.8"),
                StrokeLineJoin=PenLineJoin.Round,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round
            };
            arrow.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"MutedBrush");
            refresh.Content=arrow;
            AutomationProperties.SetName(refresh,"刷新"+title);
            warning.Visibility=Visibility.Collapsed;TitleButtons.Children.Add(warning);
            TitleButtons.Children.Add(refresh);
            Body.Children.Add(metrics);
            Render(null,null,false);
        }
        public void ConfigureMenu(MonitorApp app) {
            var menu=new ContextMenu();
            if(service==0||service==3)AddMenu(menu,"切换为悬浮球",()=>{if(SwitchDisplay!=null)SwitchDisplay();});
            AddMenu(menu,"控制中心",app.ShowControl);
            AddMenu(menu,"立即刷新",async()=>{if(pending)return;pending=true;try{await sync();}finally{pending=false;}});
            AddMenu(menu,"隐藏",()=>CloseAction());ContextMenu=menu;
        }
        static void AddMenu(ContextMenu menu,string title,Action action){var item=new MenuItem{Header=title};item.Click+=(s,e)=>action();menu.Items.Add(item);}
        public void Render(object data,string error,bool loading) {
            if(service==3)metrics.RenderQuota(data as QuotaWindow??new QuotaWindow {Label="订阅剩余额度"});
            else if(service==4){var balance=data as KimiBalance;metrics.RenderBalance(balance==null?null:balance.Amount);}
            else metrics.Render(data,service);
            UpdateStatus(data,error,loading);
            FitHeight();
        }
        public void SetAlert(string message){warning.Text=message==null?"": "⚠ "+message;warning.Visibility=message==null?Visibility.Collapsed:Visibility.Visible;FitHeight();}
        public void UpdateStatus(object data,string error,bool loading) {
            metrics.Opacity=error==null?1:0.72;
            metrics.ToolTip=error==null?null:(data==null?"":"上次读取 · ")+error;
            refresh.IsEnabled=!pending&&!loading;
        }
    }
}
