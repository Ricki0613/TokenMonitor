using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
namespace TokenMonitor {
internal sealed class KimiBalance {
 public double? Amount,Cash,Voucher;
 public string Status;
 public KimiBalance(string status){Status=status;}
 public KimiBalance(double amount){Amount=amount;Status="已同步 "+UI.Clock(DateTimeOffset.UtcNow);}
}
internal static class Kimi {
 static string FileName {get{return Path.Combine(Paths.Data,"kimi.key");}}
 public static async Task<KimiBalance> Balance(string input,bool save){
 try{
 string key=input;
 if(!save){if(!File.Exists(FileName))return new KimiBalance("尚未连接 Kimi API");byte[] clear=ProtectedData.Unprotect(File.ReadAllBytes(FileName),null,DataProtectionScope.CurrentUser);try{key=Encoding.UTF8.GetString(clear);}finally{Array.Clear(clear,0,clear.Length);}}
 if(string.IsNullOrWhiteSpace(key)||!key.Trim().StartsWith("sk-"))return new KimiBalance("请输入有效的 Kimi API Key");
 using(var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(12)}){
 client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",key.Trim());
 using(var r=await client.GetAsync("https://api.moonshot.cn/v1/users/me/balance")){
 if(!r.IsSuccessStatusCode)return new KimiBalance("余额查询失败（HTTP "+(int)r.StatusCode+"）");
 var data=Json.Child(Json.Read(await r.Content.ReadAsStringAsync()),"data");var balance=Json.Number(data,"available_balance");if(!balance.HasValue)return new KimiBalance("未返还可用余额");
 if(save){Directory.CreateDirectory(Paths.Data);byte[] bytes=Encoding.UTF8.GetBytes(key.Trim());try{Atomic.Write(FileName,ProtectedData.Protect(bytes,null,DataProtectionScope.CurrentUser));}finally{Array.Clear(bytes,0,bytes.Length);}}
 return new KimiBalance(balance.Value){Cash=Json.Number(data,"cash_balance"),Voucher=Json.Number(data,"voucher_balance")};}}
 }catch{return new KimiBalance("余额查询或保存失败，请检查网络和连接设置。");}}

 internal static QuotaWindow ParseUsage(string text) {
     if(!text.Contains("用量进度")&&!text.Contains("我的订阅"))return null;
     var match=Regex.Match(text,@"总使用量\s*[:：]?\s*(\d+(?:\.\d+)?)\s*%");
     double used;
     if(!match.Success||!double.TryParse(match.Groups[1].Value,NumberStyles.Number,CultureInfo.InvariantCulture,out used)||used>100)return null;
     var window=new QuotaWindow {Label="订阅剩余额度",Used=used};
     var reset=Regex.Match(text,@"(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2})\s*后重置");
     DateTime local;
     if(reset.Success&&DateTime.TryParseExact(reset.Groups[1].Value,"yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.AllowWhiteSpaces,out local))
         window.Reset=new DateTimeOffset(local);
     return window;
 }
 public static Task<QuotaWindow> Usage() {
     return Task.Run(()=>{
         foreach(var process in Process.GetProcessesByName("Kimi"))using(process){
             try {
                 if(process.MainWindowHandle==IntPtr.Zero)continue;
                 var root=AutomationElement.FromHandle(process.MainWindowHandle);
                 var condition=new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Text);
                 var labels=root.FindAll(TreeScope.Descendants,condition).Cast<AutomationElement>().Select(node=>node.Current.Name);
                 var reading=ParseUsage(string.Join(" ",labels));
                 if(reading!=null)return reading;
             } catch(InvalidOperationException){} catch(ElementNotAvailableException){} catch(System.Runtime.InteropServices.COMException){} catch(System.ComponentModel.Win32Exception){}
         }
         return null;
     });
 }
}}
