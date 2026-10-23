using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TokenMonitor {
    // Read only the grant written by Harness. Never persist or log its contents.
    public static class DeepSeekHarness {
        public static string CredentialPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh", ".credentials.yaml"); } }
        public static string ReadToken() {
            if(!File.Exists(CredentialPath)) throw new ProviderException("未找到 Harness 登录信息，请先在 DeepSeek Harness 中登录",60);
            try { return ParseToken(File.ReadAllText(CredentialPath)); }
            catch(ProviderException) { throw; }
            catch { throw new ProviderException("无法读取 Harness 登录信息，请确认文件可读并重新登录",60); }
        }
        // Harness emits a block mapping with scalar grant fields. Fail closed on other YAML forms.
        internal static string ParseToken(string text) {
            if(text.Length>2097152) throw InvalidGrant();
            bool records=false,account=false,payload=false;
            string token=null,issuer=null,version=null,kind=null;
            var seen=new HashSet<string>();
            foreach(string raw in text.Split('\n')) {
                string line=raw.TrimEnd('\r');
                if(string.IsNullOrWhiteSpace(line)||line.TrimStart().StartsWith("#"))continue;
                int indent=line.Length-line.TrimStart(' ').Length;
                var match=Regex.Match(line.Trim(),@"^([^:]+):(?:\s+(.*))?$");
                if(!match.Success) {if(account)throw InvalidGrant();continue;}
                string name=Scalar(match.Groups[1].Value),value=match.Groups[2].Value.Trim();
                if(indent==0) {records=name=="records"&&value=="";account=false;payload=false;continue;}
                if(indent==2) {account=records&&name=="deepseek-account-platform/default";payload=false;if(account&&!seen.Add("account"))throw InvalidGrant();continue;}
                if(!account)continue;
                if(indent==4) {
                    payload=name=="payload"&&value=="";
                    if(name=="kind") {if(!seen.Add("kind"))throw InvalidGrant();kind=Scalar(value);}
                    continue;
                }
                if(indent==6&&payload) {
                    if(!seen.Add(name))throw InvalidGrant();
                    if(name=="token")token=Scalar(value);
                    else if(name=="issuer")issuer=Scalar(value);
                    else if(name=="version")version=Scalar(value);
                }
            }
            if(kind!="grant"||version!="1"||issuer!="https://platform.deepseek.com"||string.IsNullOrWhiteSpace(token)||token.Any(char.IsWhiteSpace)||token.Any(char.IsControl))throw InvalidGrant();
            return token;
        }
        static string Scalar(string value) {
            if(value.StartsWith("\"")) {
                try {return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<string>(value);}catch {throw InvalidGrant();}
            }
            if(value.StartsWith("'")) {if(value.Length<2||!value.EndsWith("'"))throw InvalidGrant();return value.Substring(1,value.Length-2).Replace("''", "'");}
            if(value.StartsWith("|")||value.StartsWith(">")||value.StartsWith("&")||value.StartsWith("*"))throw InvalidGrant();
            return value;
        }
        static ProviderException InvalidGrant() {return new ProviderException("Harness 登录信息缺失或格式不受支持，请在 Harness 中重新登录",60);}
        internal static DeepSnapshot ParseBalance(Dictionary<string,object> response) {
            if(Json.Number(response,"code")==40003)throw new ProviderException("Harness 登录已过期，请在 DeepSeek Harness 中重新登录",120);
            var data=Json.Child(response,"data");
            if(Json.Number(response,"code")!=0||Json.Number(data,"biz_code")!=0)throw new ProviderException("Harness 官方余额查询失败，请稍后重试",60);
            var summary=Json.Child(data,"biz_data");
            var balances=new Dictionary<string,Balance>();
            AddWallets(summary,"normal_wallets",false,balances);
            AddWallets(summary,"bonus_wallets",true,balances);
            if(balances.Count==0)throw new InvalidDataException("Harness 官方未返回余额明细");
            return new DeepSnapshot {FromHarness=true,Balances=balances.Values.OrderBy(x=>x.Currency).ToList()};
        }
        static void AddWallets(Dictionary<string,object> summary,string field,bool bonus,Dictionary<string,Balance> balances) {
            var list=Json.Get(summary,field) as IList;
            if(list==null)throw new InvalidDataException("Harness 官方余额格式发生变化");
            foreach(var item in list) {
                var wallet=Json.Object(item);string currency=Json.Str(wallet,"currency");decimal amount;
                if((currency!="CNY"&&currency!="USD")||!decimal.TryParse(Json.Str(wallet,"balance"),NumberStyles.Float,CultureInfo.InvariantCulture,out amount))throw new InvalidDataException("Harness 官方余额格式发生变化");
                Balance b;if(!balances.TryGetValue(currency,out b)) {b=new Balance {Currency=currency};balances.Add(currency,b);}
                if(bonus)b.Granted+=amount;else b.ToppedUp+=amount;
                b.Total=b.Granted+b.ToppedUp;
            }
        }
    }
}

