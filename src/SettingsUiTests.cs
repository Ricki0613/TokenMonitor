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
            var interval=nodes.OfType<ComboBox>().First(x=>AutomationProperties.GetName(x)=="设置自动刷新间隔");
            var save=nodes.OfType<Button>().First(x=>Equals(x.Content,"保存密钥设置"));
            var before=File.ReadAllBytes(Paths.OpenAiKey);
            api.Password="sk-test-only-unsubmitted-change";
            int count=app.Windows.Count;
            view.SelectPage("外观设置");appearance.SelectedIndex=2;scale.SelectedIndex=3;
            checks["appearance_changes_without_save"]=Appearance.Dark&&app.Prefs.Appearance=="dark"&&Preferences.Load().Appearance=="dark";
            checks["orb_size_saves_immediately"]=app.Prefs.OrbScale==150&&Preferences.Load().OrbScale==150&&Math.Abs(app.Orb.Width-114)<1;
            interval.SelectedIndex=2;view.UpdateLayout();checks["interval_applies_immediately"]=app.Prefs.Interval==60&&app.Main.Interval.SelectedIndex==2;interval.SelectedIndex=1;
            checks["appearance_has_no_save_button"]=save.Visibility==Visibility.Collapsed;
            checks["appearance_does_not_commit_pending_key"]=before.SequenceEqual(File.ReadAllBytes(Paths.OpenAiKey));
            view.SelectPage("OpenAI 密钥");checks["navigation_keeps_unsaved_key"]=api.Password=="sk-test-only-unsubmitted-change"&&save.Visibility==Visibility.Visible;
            api.Clear();
            view.SelectPage("关于软件");app.Main.RenderUpdate(null,"模拟更新状态");view.UpdateLayout();
            checks["about_contains_live_update_status"]=Walk(view).OfType<TextBlock>().Any(x=>x.Text=="模拟更新状态");
            checks["settings_navigation_stays_in_main_window"]=app.Windows.Count==count&&Window.GetWindow(view)==app.Main;
            checks["about_has_no_save_button"]=save.Visibility==Visibility.Collapsed;
            view.SelectPage("外观设置");appearance.SelectedIndex=1;scale.SelectedIndex=1;
            checks["light_theme_also_applies_immediately"]=!Appearance.Dark&&Preferences.Load().Appearance=="light";
            view.SelectPage("OpenAI 密钥");
        }
        public static IEnumerable<DependencyObject> Walk(DependencyObject node) {
            yield return node;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Walk(VisualTreeHelper.GetChild(node,i)))yield return child;
        }
    }
}
