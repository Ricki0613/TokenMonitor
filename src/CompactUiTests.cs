using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TokenMonitor {
    public static class CompactUiTests {
        static void DoubleClick(Window window) {
            window.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left) {
                RoutedEvent=Control.PreviewMouseDoubleClickEvent,Source=window
            });
        }
        public static async Task Run(MonitorApp app,IDictionary<string,object> checks,string folder) {
            foreach(string theme in new[]{"light","dark"}) {
                Appearance.Apply(theme);
                checks["brand_exact_"+theme]=((SolidColorBrush)app.Resources["GreenBrush"]).Color==UI.Brush(Brand.Mint).Color
                    &&((SolidColorBrush)app.Resources["BlueBrush"]).Color==UI.Brush(Brand.Violet).Color;
            }
            app.Main.GptSwitch.IsChecked=true;app.SetOrbMode(false);
            DoubleClick(app.Gpt);
            checks["gpt_double_click_to_orb"]=app.Orb.IsVisible&&!app.Gpt.IsVisible&&app.Prefs.OrbMode;
            DoubleClick(app.Orb);
            checks["gpt_double_click_to_window"]=!app.Orb.IsVisible&&app.Gpt.IsVisible&&!app.Prefs.OrbMode;
            app.SetKimiVisible(true,false);app.SetKimiOrbMode(false);
            DoubleClick(app.KimiWindow);
            checks["kimi_double_click_to_orb"]=app.KimiOrb.IsVisible&&!app.KimiWindow.IsVisible;
            DoubleClick(app.KimiOrb);
            checks["kimi_double_click_to_window"]=!app.KimiOrb.IsVisible&&app.KimiWindow.IsVisible;
            app.Gpt.Render(app.GptData,null,false);
            foreach(string size in new[]{"small","medium","large"}) {
                FontSizing.Apply(size);
                foreach(var window in new[]{app.Gpt,app.Deep,app.OpenAi,app.KimiWindow,app.KimiApiWindow}) {
                    window.Show();window.FitToContent();
                    await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                    window.UpdateLayout();
                    string key=window.Title+"_"+size;
                    checks["float_fit_"+key]=Math.Abs(window.Scroll.ViewportHeight-window.Scroll.ExtentHeight)<2
                        &&window.Scroll.ExtentWidth<=window.Scroll.ViewportWidth+1;
                    double width=window.ActualWidth,height=window.ActualHeight;
                    window.ResizeBy(1,1,500,500);window.UpdateLayout();
                    checks["float_locked_"+key]=window.ResizeMode==ResizeMode.NoResize&&width==window.ActualWidth&&height==window.ActualHeight
                        &&!SettingsUiTests.Walk(window).OfType<System.Windows.Controls.Primitives.Thumb>().Any();
                    checks["float_menu_"+key]=window.ContextMenu!=null&&window.ContextMenu.Items.Count==(window==app.Gpt||window==app.KimiWindow?4:3);
                    UI.SaveImage(window,Path.Combine(folder,"compact-"+key+".png"));
                }
                app.Main.ShowOverview();app.Main.FitHeight();
                await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                UI.SaveImage(app.Main,Path.Combine(folder,"fitted-home-"+size+".png"));
                foreach(string page in new[]{"外观设置","ChatGPT 连接","DeepSeek API","Kimi 订阅","Kimi API","OpenAI API","数据目录","关于软件"}) {
                    app.Main.OpenSettings(page);
                    await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
                    app.Main.UpdateLayout();
                    var scroll=app.Main.SettingsPage.Scroll;
                    checks["settings_fit_"+size+page]=scroll.ExtentWidth<=scroll.ViewportWidth+1
                        &&app.Main.Width==Math.Min(920,SystemParameters.WorkArea.Width)&&app.Main.Height==Math.Min(720,SystemParameters.WorkArea.Height);
                    UI.SaveImage(app.Main,Path.Combine(folder,"fitted-"+size+"-"+page+".png"));
                }
            }
            app.Gpt.ContextMenu.PlacementTarget=app.Gpt;app.Gpt.ContextMenu.IsOpen=true;
            await app.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
            UI.SaveImage(app.Gpt.ContextMenu,Path.Combine(folder,"float-menu.png"));app.Gpt.ContextMenu.IsOpen=false;
            FontSizing.Apply("medium");Appearance.Apply("light");app.Main.ShowOverview();
        }
    }
}
