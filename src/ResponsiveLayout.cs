using System;
using System.Windows;
using System.Windows.Controls;

namespace TokenMonitor {
    // Reflow whole cards; never scale text or shrink a Viewbox.
    public sealed class CardPanel : Panel {
        const double Gap=10;
        int Columns(double width) {return double.IsInfinity(width)?1:Math.Max(1,Math.Min(InternalChildren.Count,Math.Min(3,(int)((width+Gap)/280))));}
        protected override Size MeasureOverride(Size available) {
            int cols=Columns(available.Width);double width=double.IsInfinity(available.Width)?320:Math.Max(0,(available.Width-Gap*(cols-1))/cols);
            double height=0,row=0;
            for(int i=0;i<InternalChildren.Count;i++) {
                var child=InternalChildren[i];child.Measure(new Size(width,double.PositiveInfinity));row=Math.Max(row,child.DesiredSize.Height);
                if(i%cols==cols-1||i==InternalChildren.Count-1) {height+=row+(i==InternalChildren.Count-1?0:Gap);row=0;}
            }
            return new Size(double.IsInfinity(available.Width)?width:available.Width,height);
        }
        protected override Size ArrangeOverride(Size final) {
            int cols=Columns(final.Width);double width=Math.Max(0,(final.Width-Gap*(cols-1))/cols),y=0;
            for(int start=0;start<InternalChildren.Count;start+=cols) {
                double height=0;for(int i=start;i<Math.Min(start+cols,InternalChildren.Count);i++)height=Math.Max(height,InternalChildren[i].DesiredSize.Height);
                for(int i=start;i<Math.Min(start+cols,InternalChildren.Count);i++)InternalChildren[i].Arrange(new Rect((i-start)*(width+Gap),y,width,height));
                y+=height+Gap;
            }
            return final;
        }
    }
}
