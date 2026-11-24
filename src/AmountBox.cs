using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TokenMonitor {
    public sealed class AmountBox : TextBox {
        string previous="";
        bool restoring;
        public AmountBox() {
            PreviewTextInput+=(s,e)=>{if(!Valid(Replacement(e.Text)))e.Handled=true;};
            DataObject.AddPastingHandler(this,(s,e)=>{
                if(!e.DataObject.GetDataPresent(DataFormats.UnicodeText)||!Valid(Replacement(e.DataObject.GetData(DataFormats.UnicodeText) as string)))e.CancelCommand();
            });
            TextChanged+=(s,e)=>{
                if(restoring)return;
                if(Valid(Text)){previous=Text;return;}
                int position=Math.Max(0,Math.Min(SelectionStart,previous.Length));restoring=true;Text=previous;SelectionStart=position;restoring=false;
            };
        }
        string Replacement(string input){return Text.Remove(SelectionStart,SelectionLength).Insert(SelectionStart,input??"");}
        public static bool Valid(string value){return value!=null&&value.Count(c=>c=='.')<=1&&value.All(c=>(c>='0'&&c<='9')||c=='.');}
    }
}
