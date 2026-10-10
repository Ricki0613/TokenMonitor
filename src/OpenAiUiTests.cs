using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace TokenMonitor {
    public static class OpenAiUiTests {
        public static void Run(SettingsView view,IDictionary<string,object> checks) {
            var nodes=Walk(view).ToList();
            var api=nodes.OfType<PasswordBox>().FirstOrDefault(x=>AutomationProperties.GetName(x)=="新的 OpenAI API Key");
            var admin=nodes.OfType<PasswordBox>().FirstOrDefault(x=>AutomationProperties.GetName(x)=="新的 OpenAI Admin API Key");
            var id=nodes.OfType<TextBox>().FirstOrDefault(x=>AutomationProperties.GetName(x)=="OpenAI API Key ID，key_ 开头");
            checks["openai_settings_fields_accessible"]=api!=null&&admin!=null&&id!=null;
            if(api==null||admin==null||id==null)return;
            checks["openai_saved_keys_stay_masked_and_blank"]=api.Password.Length==0&&admin.Password.Length==0;
            checks["openai_saved_key_id_loaded"]=id.Text=="key_ui_test";
            bool exposed=nodes.OfType<TextBlock>().Any(x=>ContainsSecret(x.Text))||nodes.OfType<TextBox>().Any(x=>ContainsSecret(x.Text));
            checks["openai_saved_keys_not_in_visible_text"]=!exposed;
            api.Password="sk-test-only-ui-replacement";
            checks["openai_replacing_key_clears_old_attribution"]=id.Text.Length==0;
            id.Text="key_ui_replacement";api.Password="sk-test-only-ui-replacement-final";
            checks["openai_replacement_id_preserved_while_editing"]=id.Text=="key_ui_replacement";
            api.Clear();id.Text="key_ui_test";
        }
        static bool ContainsSecret(string text) {
            return text!=null&&(text.Contains("sk-test-only-ui-openai-key")||text.Contains("sk-admin-test-only-ui-openai-key"));
        }
        static IEnumerable<DependencyObject> Walk(DependencyObject node) {
            yield return node;
            int count=VisualTreeHelper.GetChildrenCount(node);
            for(int i=0;i<count;i++)foreach(var child in Walk(VisualTreeHelper.GetChild(node,i)))yield return child;
        }
    }
}
