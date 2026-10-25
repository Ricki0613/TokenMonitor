using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace TokenMonitor {
    public static class DashboardUiTests {
        public static async Task Run(MonitorApp app,IDictionary<string,object> checks,string folder) {
            var main=app.Main;
            main.ShowOverview();app.UpdateLabels();main.UpdateLayout();
            var text=SettingsUiTests.Walk(main).OfType<TextBlock>().Select(x=>x.Text).ToList();
            checks["dashboard_two_remaining_percentages"]=text.Contains("72%")&&text.Contains("39%");
            checks["dashboard_today_and_month"]=text.Contains("150,200")&&text.Contains("5,098,940");
            checks["dashboard_connection_actions"]=SettingsUiTests.Walk(main).OfType<Button>().Count(x=>Equals(x.Content,"连接设置"))==3;
            main.GptSwitch.IsChecked=false;main.DeepSwitch.IsChecked=false;main.OpenAiSwitch.IsChecked=false;
            checks["hidden_floats_keep_monitoring"]=app.MonitoringGpt&&app.MonitoringDeep&&app.MonitoringOpenAi&&!app.Gpt.IsVisible&&!app.Deep.IsVisible&&!app.OpenAi.IsVisible;
            await app.RefreshAll(true);
            main.GptSwitch.IsChecked=true;main.DeepSwitch.IsChecked=true;main.OpenAiSwitch.IsChecked=true;
            foreach(var view in new Shell[]{main,app.Gpt,app.Deep,app.OpenAi}) {
                view.AutoHeight=false;view.Width=view==main?1200:420;view.Height=900;await Task.Delay(150);
                await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);view.UpdateLayout();
                checks[view.Title+"_tall_fills_viewport"]=Math.Abs(view.Scroll.ViewportHeight-view.Body.Children.Cast<FrameworkElement>().Sum(x=>x.DesiredSize.Height))<3;
                checks[view.Title+"_geometry"]=new[]{view.Height,view.ActualHeight,view.MaxHeight,view.Body.DesiredSize.Height,view.Scroll.ViewportHeight};
                UI.SaveImage(view,Path.Combine(folder,view==main?"dashboard-tall.png":view==app.Gpt?"gpt-tall.png":view==app.Deep?"deep-tall.png":"openai-tall.png"));
            }
            main.Width=760;main.Height=560;await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);main.UpdateLayout();
            checks["dashboard_narrow_scrolls_without_horizontal_overflow"]=main.Scroll.ExtentWidth<=main.Scroll.ViewportWidth+1;
            UI.SaveImage(main,Path.Combine(folder,"dashboard-narrow.png"));
            main.Width=1040;main.Height=760;
            app.Gpt.Width=368;app.Gpt.FitToContent();await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
            checks["compact_height"]=app.Gpt.Height;checks["compact_body"]=app.Gpt.Body.DesiredSize.Height;
            checks["tall_float_returns_to_compact"]=app.Gpt.Height<750&&app.Gpt.AutoHeight;
            app.GptData=null;app.DeepData=null;app.OpenAiData=null;app.FirstRun=true;app.Prefs.Gpt=false;app.Prefs.Deep=false;app.Prefs.OpenAi=false;app.Prefs.MonitorGpt=false;app.Prefs.MonitorDeep=false;app.Prefs.MonitorOpenAi=false;
            var fresh=new MainView(app);fresh.Show();await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
            checks["first_run_no_selected_floats"]=fresh.GptSwitch.IsChecked==false&&fresh.DeepSwitch.IsChecked==false&&fresh.OpenAiSwitch.IsChecked==false;
            checks["first_run_guidance"]=SettingsUiTests.Walk(fresh).OfType<TextBlock>().Any(x=>x.Text.Contains("先选择要监控的服务"));
            UI.SaveImage(fresh,Path.Combine(folder,"first-run.png"));fresh.Exiting=true;fresh.Close();
            checks["openai_next_step_admin"]=OpenAiConnectionHelp.Next(new OpenAiConnection()).Contains("添加管理员");
            checks["openai_next_step_id"]=OpenAiConnectionHelp.Next(new OpenAiConnection {AdminKey="test"}).Contains("key_");
        }
    }
}
