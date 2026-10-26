using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace TokenMonitor {
    public static class FontSizing {
        static readonly HashSet<double> sizes=new HashSet<double> {12,13};
        static double offset=1.5;
        static string Key(double size) {return "Font"+size.ToString(CultureInfo.InvariantCulture);}
        public static void Bind(FrameworkElement element,DependencyProperty property,double size) {
            sizes.Add(size);Application.Current.Resources[Key(size)]=size+offset;
            element.SetResourceReference(property,Key(size));
        }
        public static void Apply(string choice) {
            offset=choice=="small"?0:choice=="large"?3:1.5;
            foreach(double size in sizes)Application.Current.Resources[Key(size)]=size+offset;
        }
    }
}
