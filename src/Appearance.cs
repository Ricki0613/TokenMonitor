using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace TokenMonitor {
    public static class Appearance {
        static readonly Dictionary<string,string> keys=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            {"#202526","InkBrush"},{"#7B838D","MutedBrush"},{"#FFFFFF","CardBrush"},
            {"#F5F7F8","SurfaceBrush"},{"#E2E6E8","BorderBrush"},{"#F0F2F4","ButtonBrush"},
            {"#48515A","ButtonInkBrush"},{"#DCE1E6","InputBorderBrush"},{"#E9EEEB","TrackBrush"},
            {"#EEF0F3","DividerBrush"},{"#10A37F","GreenBrush"},{"#4D6BFE","BlueBrush"},
            {"#B77436","WarningBrush"},{"#CA6547","DangerBrush"},{"#DD6A42","DangerBrush"}
        };
        public static bool Dark {get;private set;}
        public static bool SystemDark() {
            try {using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                return key!=null&&Convert.ToInt32(key.GetValue("AppsUseLightTheme",1))==0;}
            catch {return false;}
        }
        public static void Apply(string preference) {
            Dark=preference=="dark"||(preference=="system"&&SystemDark());
            string[] names={"Ink","Muted","Card","Surface","Border","Button","ButtonInk","InputBorder","Track","Divider","Green","Blue","Warning","Danger","Action","ActionInk","SwitchOff"};
            string[] light={"#202526","#68727D","#FFFFFF","#F5F7F8","#E2E6E8","#E9EDF1","#48515A","#DCE1E6","#E9EEEB","#EEF0F3","#10A37F","#4D6BFE","#A46023","#C75637","#202526","#FFFFFF","#D9DEE3"};
            string[] dark={"#F1F4F6","#A7B0BC","#252B33","#191E25","#38414D","#343D49","#E1E7EF","#536071","#3A453F","#3A4350","#36C9A3","#8EA0FF","#F2B575","#FF9779","#DCE5EF","#19212B","#536071"};
            for(int i=0;i<names.Length;i++) {
                var brush=UI.Brush((Dark?dark:light)[i]);brush.Freeze();
                Application.Current.Resources[names[i]+"Brush"]=brush;
            }
        }
        public static void Paint(FrameworkElement element,DependencyProperty property,string color) {
            string key;if(keys.TryGetValue(color,out key))element.SetResourceReference(property,key);
            else element.SetValue(property,UI.Brush(color));
        }
    }
}
