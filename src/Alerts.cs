using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TokenMonitor {
    public static class EdgeSnap {
        public static void Apply(Window window) {
            var source=PresentationSource.FromVisual(window);
            var m=source==null?Matrix.Identity:source.CompositionTarget.TransformToDevice;
            var area=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)((window.Left+window.Width/2)*m.M11),(int)((window.Top+window.Height/2)*m.M22))).WorkingArea;
            double left=area.Left/m.M11,right=area.Right/m.M11,top=area.Top/m.M22,bottom=area.Bottom/m.M22;
            const double distance=18;
            if(Math.Abs(window.Left-left)<distance)window.Left=left;
            else if(Math.Abs(window.Left+window.Width-right)<distance)window.Left=right-window.Width;
            if(Math.Abs(window.Top-top)<distance)window.Top=top;
            else if(Math.Abs(window.Top+window.Height-bottom)<distance)window.Top=bottom-window.Height;
        }
    }
    public sealed class AlertTracker {
        readonly Dictionary<string,int> levels=new Dictionary<string,int>();
        public bool Changed(string key,int level) {
            int previous;bool exists=levels.TryGetValue(key,out previous);levels[key]=level;
            return level>0&&(!exists||level>previous);
        }
        public static int Quota(double? remaining){return !remaining.HasValue?0:remaining.Value<5?3:remaining.Value<10?2:remaining.Value<20?1:0;}
        public static int Balance(double? value,double? threshold){return value.HasValue&&threshold.HasValue&&value.Value<threshold.Value?1:0;}
    }
}
