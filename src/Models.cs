using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace TokenMonitor {
    public static class Json {
        public static Dictionary<string, object> Object(object value) { return value as Dictionary<string, object>; }
        public static object Get(Dictionary<string, object> obj, string key) { object value; return obj != null && obj.TryGetValue(key, out value) ? value : null; }
        public static Dictionary<string, object> Child(Dictionary<string, object> obj, string key) { return Object(Get(obj, key)); }
        public static string Str(Dictionary<string, object> obj, string key) { var v = Get(obj, key); return v == null ? null : Convert.ToString(v, CultureInfo.InvariantCulture); }
        public static double? Number(Dictionary<string, object> obj, string key) { double n; return double.TryParse(Str(obj,key), NumberStyles.Float, CultureInfo.InvariantCulture, out n) && !double.IsNaN(n) && !double.IsInfinity(n) ? (double?)n : null; }
        public static Dictionary<string, object> Read(string s) { return new JavaScriptSerializer { MaxJsonLength = 2097152 }.Deserialize<Dictionary<string, object>>(s); }
        public static string Write(object obj) { return new JavaScriptSerializer().Serialize(obj); }
    }
    public class QuotaWindow {
        public string Label;
        public double? Used;
        public DateTimeOffset? Reset;
        public double? Remaining { get { return Used.HasValue ? (double?)Math.Max(0,100-Used.Value) : null; } }
    }
    public class QuotaGroup {
        public string Id, Name, Plan, Credits;
        public List<QuotaWindow> Windows = new List<QuotaWindow>();
    }
    public class GptSnapshot {
        public List<QuotaGroup> Groups = new List<QuotaGroup>();
        public int? ResetCredits;
        public DateTimeOffset Fetched = DateTimeOffset.UtcNow;
        public static GptSnapshot Parse(Dictionary<string, object> obj) {
            var result = new GptSnapshot();
            var map = Json.Child(obj,"rateLimitsByLimitId");
            if (map != null && map.Count > 0) {
                foreach(var item in map.OrderBy(x=>x.Key=="codex"?0:1).ThenBy(x=>x.Key)) {
                    var bucket = Json.Object(item.Value);
                    if(bucket != null) result.Groups.Add(ParseGroup(bucket,item.Key));
                }
            } else {
                var bucket=Json.Child(obj,"rateLimits");
                if(bucket!=null) result.Groups.Add(ParseGroup(bucket,Json.Str(bucket,"limitId")??"codex"));
            }
            if(result.Groups.Count==0) throw new InvalidDataException("官方尚未返回额度数据");
            var resetCount=Json.Number(Json.Child(obj,"rateLimitResetCredits"),"availableCount");
            if(resetCount.HasValue && resetCount>=0 && resetCount<=int.MaxValue && resetCount==Math.Truncate(resetCount.Value)) result.ResetCredits=(int)resetCount.Value;
            return result;
        }
        static QuotaGroup ParseGroup(Dictionary<string,object> obj,string id) {
            var group=new QuotaGroup { Id=id, Name=Json.Str(obj,"limitName"), Plan=Json.Str(obj,"planType") };
            if(string.IsNullOrWhiteSpace(group.Name)) group.Name=id=="codex"?"Codex / ChatGPT Work":id;
            AddWindow(group,Json.Child(obj,"primary"),"当前额度");
            AddWindow(group,Json.Child(obj,"secondary"),"周期额度");
            var credits=Json.Child(obj,"credits");
            if(credits!=null) group.Credits=Equals(Json.Get(credits,"unlimited"),true)?"不限量":Json.Str(credits,"balance");
            return group;
        }
        static void AddWindow(QuotaGroup group,Dictionary<string,object> obj,string fallback) {
            if(obj==null) return;
            var mins=Json.Number(obj,"windowDurationMins");
            var label=mins==300?"5 小时额度":mins==10080?"每周额度":mins==1440?"每日额度":
                mins.HasValue && mins>0 ? (mins.Value%60==0?(mins.Value/60).ToString("0.##")+" 小时额度":mins.Value.ToString("0.##")+" 分钟额度") : fallback;
            var used=Json.Number(obj,"usedPercent");
            if(used.HasValue && used.Value<0) used=null;
            var win=new QuotaWindow { Label=label, Used=used };
            var reset=Json.Number(obj,"resetsAt");
            if(reset.HasValue) { try { win.Reset=new DateTimeOffset(1970,1,1,0,0,0,TimeSpan.Zero).AddSeconds(reset.Value); } catch(ArgumentOutOfRangeException) {} }
            group.Windows.Add(win);
        }
    }
    public class Balance {
        public string Currency;
        public decimal Total, Granted, ToppedUp;
        public string Symbol { get { return Currency=="CNY"?"¥":Currency=="USD"?"$":Currency+" "; } }
        public string Format(decimal value) { return Symbol+value.ToString("0.00####",CultureInfo.InvariantCulture); }
    }
    public class DeepSnapshot {
        public bool Available;
        public List<Balance> Balances=new List<Balance>();
        public DateTimeOffset Fetched=DateTimeOffset.UtcNow;
        public static DeepSnapshot Parse(Dictionary<string,object> obj) {
            if(!(Json.Get(obj,"is_available") is bool)) throw new InvalidDataException("官方余额格式发生变化");
            var result=new DeepSnapshot { Available=(bool)Json.Get(obj,"is_available") };
            var list=Json.Get(obj,"balance_infos") as IEnumerable;
            if(list==null) throw new InvalidDataException("官方未返回余额明细");
            foreach(var item in list) {
                var b=Json.Object(item); decimal total,grant,top;
                if(b==null || string.IsNullOrWhiteSpace(Json.Str(b,"currency")) ||
                   !decimal.TryParse(Json.Str(b,"total_balance"),NumberStyles.Number,CultureInfo.InvariantCulture,out total) ||
                   !decimal.TryParse(Json.Str(b,"granted_balance"),NumberStyles.Number,CultureInfo.InvariantCulture,out grant) ||
                   !decimal.TryParse(Json.Str(b,"topped_up_balance"),NumberStyles.Number,CultureInfo.InvariantCulture,out top))
                    throw new InvalidDataException("官方余额格式发生变化");
                result.Balances.Add(new Balance { Currency=Json.Str(b,"currency"),Total=total,Granted=grant,ToppedUp=top });
            }
            if(result.Balances.Count==0) throw new InvalidDataException("官方未返回余额明细");
            return result;
        }
    }
    public class Preferences {
        public bool Gpt=true, Deep=true;
        public int Interval=30;
        public string Appearance="system", PendingResetKey;
        public double GptWidth=368,GptHeight=500,DeepWidth=368,DeepHeight=445;
        public bool GptAutoHeight=true,DeepAutoHeight=true;
        public double GptX=-1,GptY=-1,DeepX=-1,DeepY=-1,MainX=-1,MainY=-1;
        public static Preferences Load() {
            try { var p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Paths.Preferences));
                if(p==null)return new Preferences();
                p.Interval=new[]{15,30,60,120}.Contains(p.Interval)?p.Interval:30;
                if(!new[]{"system","light","dark"}.Contains(p.Appearance))p.Appearance="system";
                p.GptWidth=Size(p.GptWidth,300,1600,368);p.DeepWidth=Size(p.DeepWidth,300,1600,368);
                p.GptHeight=Size(p.GptHeight,210,1600,500);p.DeepHeight=Size(p.DeepHeight,210,1600,445);
                Guid key;if(!Guid.TryParse(p.PendingResetKey,out key))p.PendingResetKey=null;
                return p; }
            catch { return new Preferences(); }
        }
        static double Size(double value,double min,double max,double fallback) {return double.IsNaN(value)||double.IsInfinity(value)||value<min?fallback:Math.Min(max,value);}
        public void Save() { Directory.CreateDirectory(Paths.Data); Atomic.Write(Paths.Preferences,Encoding.UTF8.GetBytes(Json.Write(this))); }
    }
    public sealed class ResetResult {
        public readonly string Outcome;
        public bool Success {get {return Outcome=="reset"||Outcome=="alreadyRedeemed";}}
        ResetResult(string outcome) {Outcome=outcome;}
        public static ResetResult Parse(Dictionary<string,object> data) {
            var outcome=Json.Str(data,"outcome");
            if(!new[]{"reset","alreadyRedeemed","nothingToReset","noCredit"}.Contains(outcome))
                throw new ProviderException("重置结果未知，请重试确认上次结果");
            return new ResetResult(outcome);
        }
    }
    public static class Paths {
        public static string Root=AppDomain.CurrentDomain.BaseDirectory;
        public static string Data { get { return Path.Combine(Root,"data"); } }
        public static string Preferences { get { return Path.Combine(Data,"settings.json"); } }
        public static string Key { get { return Path.Combine(Data,"deepseek.key"); } }
    }
    public static class Atomic {
        public static void Write(string path,byte[] bytes) {
            string temp=path+".tmp"; File.WriteAllBytes(temp,bytes);
            if(File.Exists(path)) File.Replace(temp,path,null); else File.Move(temp,path);
        }
    }
    public static class SecretStore {
        static readonly byte[] Entropy=Encoding.UTF8.GetBytes("TokenMonitor.DeepSeek.v1");
        public static void Save(string key) {
            if(string.IsNullOrWhiteSpace(key)||!key.Trim().StartsWith("sk-")||key.Trim().Length<16) throw new ArgumentException("请输入有效的 DeepSeek API Key");
            byte[] plain=Encoding.UTF8.GetBytes(key.Trim());
            try { Directory.CreateDirectory(Paths.Data);Atomic.Write(Paths.Key,ProtectedData.Protect(plain,Entropy,DataProtectionScope.CurrentUser)); }
            finally { Array.Clear(plain,0,plain.Length); }
        }
        public static string Read() {
            if(!File.Exists(Paths.Key)) throw new InvalidOperationException("请先在设置中填写 DeepSeek API Key");
            byte[] plain=ProtectedData.Unprotect(File.ReadAllBytes(Paths.Key),Entropy,DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plain); } finally { Array.Clear(plain,0,plain.Length); }
        }
    }
}
