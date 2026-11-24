using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokenMonitor {
    public static class SettingsUiTests {
        public static void Run(MonitorApp app,IDictionary<string,object> checks) {
            var view=app.Main.SettingsPage;var nodes=Walk(view).ToList();
            var api=nodes.OfType<PasswordBox>().First(x=>AutomationProperties.GetName(x)=="新的 OpenAI API Key");
            var appearance=nodes.OfType<ComboBox>().First(x=>AutomationProperties.GetName(x)=="外观主题");
            var scale=nodes.OfType<ComboBox>().First(x=>AutomationProperties.GetName(x)=="ChatGPT 悬浮球大小");
            var interval=app.Main.Interval;
            checks["appearance_excludes_refresh_interval"]=!nodes.OfType<TextBlock>().Any(x=>x.Text=="自动刷新");
            var save=nodes.OfType<Button>().First(x=>Equals(x.Content,"保存连接设置"));
            var before=File.ReadAllBytes(Paths.OpenAiKey);
            api.Password="sk-test-only-unsubmitted-change";
            int count=app.Windows.Count;
            view.SelectPage("外观设置");appearance.SelectedIndex=2;scale.SelectedIndex=3;
            checks["appearance_changes_without_save"]=Appearance.Dark&&app.Prefs.Appearance=="dark"&&Preferences.Load().Appearance=="dark";
            checks["orb_size_saves_immediately"]=app.Prefs.OrbScale==150&&Preferences.Load().OrbScale==150&&Math.Abs(app.Orb.Width-114)<1;
            interval.SelectedIndex=2;view.UpdateLayout();checks["interval_applies_immediately"]=app.Prefs.Interval==60&&app.Main.Interval.SelectedIndex==2;interval.SelectedIndex=1;
            var opacity=nodes.OfType<Slider>().First(x=>AutomationProperties.GetName(x)=="悬浮窗与悬浮球不透明度");opacity.Value=40;checks["opacity_all_float_surfaces"]=new Window[]{app.Gpt,app.Deep,app.OpenAi,app.KimiWindow,app.KimiApiWindow,app.Orb,app.KimiOrb}.All(w=>Math.Abs(w.Opacity-0.67)<0.001);opacity.Value=100;
            view.SelectPage("提醒设置");var amount=nodes.OfType<AmountBox>().First();amount.Text="12.5";amount.Text="12.5abc";checks["amount_rejects_invalid_input"]=amount.Text=="12.5"&&!AmountBox.Valid("1.2.3")&&!AmountBox.Valid("-1")&&!AmountBox.Valid("1e3")&&AmountBox.Valid("0.25")&&AmountBox.Valid("");amount.Text="";checks["alerts_separate_page"]=view.SelectedPage=="提醒设置"&&nodes.OfType<CheckBox>().Any(x=>Equals(x.Content,"发送低额度系统通知")&&x.IsVisible);view.SelectPage("外观设置");
            var font=nodes.OfType<ComboBox>().First(x=>AutomationProperties.GetName(x)=="界面字号");font.SelectedIndex=2;checks["font_size_persisted"]=Preferences.Load().FontSize=="large";font.SelectedIndex=1;
            checks["appearance_has_no_save_button"]=save.Visibility==Visibility.Collapsed;
            checks["appearance_does_not_commit_pending_key"]=before.SequenceEqual(File.ReadAllBytes(Paths.OpenAiKey));
            view.SelectPage("OpenAI API");checks["navigation_keeps_unsaved_key"]=api.Password=="sk-test-only-unsubmitted-change"&&save.Visibility==Visibility.Visible;
            api.Clear();
            view.SelectPage("关于软件");app.Main.RenderUpdate(null,"模拟更新状态");view.UpdateLayout();
            checks["about_contains_live_update_status"]=Walk(view).OfType<TextBlock>().Any(x=>x.Text=="模拟更新状态");
            checks["settings_navigation_stays_in_main_window"]=app.Windows.Count==count&&Window.GetWindow(view)==app.Main;
            checks["about_has_no_save_button"]=save.Visibility==Visibility.Collapsed;
            view.SelectPage("外观设置");appearance.SelectedIndex=1;scale.SelectedIndex=1;
            checks["light_theme_also_applies_immediately"]=!Appearance.Dark&&Preferences.Load().Appearance=="light";
            view.SelectPage("OpenAI API");
        }
        public static IEnumerable<DependencyObject> Walk(DependencyObject node) {
            yield return node;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Walk(VisualTreeHelper.GetChild(node,i)))yield return child;
        }
    }
}
