using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TokenMonitor {
    public static class SelfTests {
        public static int Run(string report) {
            var passed=new List<string>();var failed=new List<string>();
            Action<string,Action> check=(name,body)=>{try{body();passed.Add(name);}catch(Exception e){failed.Add(name+": "+e.Message);}};
            check("GPT uses server windows and remaining percentages",()=>{
                var s=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"primary\":{\"usedPercent\":28,\"windowDurationMins\":300,\"resetsAt\":1791360748},\"secondary\":{\"usedPercent\":61,\"windowDurationMins\":10080}}}"));
                Assert(s.Groups[0].Windows[0].Remaining==72&&s.Groups[0].Windows[1].Remaining==39);
                Assert(s.Groups[0].Windows[0].Label=="5 小时额度"&&s.Groups[0].Windows[1].Label=="每周额度");
                Assert(s.Groups[0].Windows[0].Reset.Value.ToUnixTimeSeconds()==1791360748);
            });
            check("Multi-bucket response takes precedence without duplicate legacy bucket",()=>{
                var s=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"primary\":{\"usedPercent\":99}},\"rateLimitsByLimitId\":{\"codex_other\":{\"primary\":{\"usedPercent\":12,\"windowDurationMins\":15}},\"codex\":{\"secondary\":{\"usedPercent\":5,\"windowDurationMins\":10080}}}}"));
                Assert(s.Groups.Count==2&&s.Groups[0].Id=="codex"&&s.Groups[0].Windows[0].Remaining==95&&s.Groups[1].Windows[0].Label=="15 分钟额度");
            });
            check("Missing percentages and reset times stay unknown",()=>{
                var s=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"primary\":{},\"secondary\":null}}"));
                Assert(s.Groups[0].Windows.Count==1&&!s.Groups[0].Windows[0].Remaining.HasValue&&!s.Groups[0].Windows[0].Reset.HasValue);
            });
            check("Unavailable GPT data is not reported as zero usage",()=>{
                bool thrown=false;try{GptSnapshot.Parse(Json.Read("{}"));}catch(InvalidDataException){thrown=true;}Assert(thrown);
            });
            check("Over-quota stays at zero remaining",()=>{
                var s=GptSnapshot.Parse(Json.Read("{\"rateLimits\":{\"primary\":{\"usedPercent\":102}}}"));Assert(s.Groups[0].Windows[0].Remaining==0);
            });
            check("DeepSeek preserves CNY and USD precision independently",()=>{
                var s=DeepSnapshot.Parse(Json.Read("{\"is_available\":true,\"balance_infos\":[{\"currency\":\"CNY\",\"total_balance\":\"51.7801\",\"granted_balance\":\"1.00\",\"topped_up_balance\":\"50.7801\"},{\"currency\":\"USD\",\"total_balance\":\"0.10\",\"granted_balance\":\"0\",\"topped_up_balance\":\"0.10\"}]}"));
                Assert(s.Balances.Count==2&&s.Balances[0].Total==51.7801m&&s.Balances[0].Format(s.Balances[0].Total)=="¥51.7801"&&s.Balances[1].Format(0.1m)=="$0.10");
            });
            check("Malformed DeepSeek balance is rejected, not coerced to zero",()=>{
                bool thrown=false;try{DeepSnapshot.Parse(Json.Read("{\"is_available\":true,\"balance_infos\":[{\"currency\":\"CNY\",\"total_balance\":\"bad\"}]}"));}catch(InvalidDataException){thrown=true;}Assert(thrown);
            });
            check("DeepSeek insufficient balance status is preserved",()=>{
                var s=DeepSnapshot.Parse(Json.Read("{\"is_available\":false,\"balance_infos\":[{\"currency\":\"CNY\",\"total_balance\":\"0\",\"granted_balance\":\"0\",\"topped_up_balance\":\"0\"}]}"));Assert(!s.Available&&s.Balances[0].Total==0);
            });
            check("Beijing display timezone is independent of PC timezone",()=>Assert(UI.China(new DateTimeOffset(2026,10,7,0,0,0,TimeSpan.Zero)).Hour==8));
            var saved=Paths.Root;var temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"test-data-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);Paths.Root=temp;
            try {
                check("DPAPI credential round-trip and no plaintext at rest",()=>{
                    string fake="sk-test-only-not-a-real-secret";SecretStore.Save(fake);Assert(SecretStore.Read()==fake);
                    Assert(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Paths.Key)).Contains(fake));
                });
                check("Visibility and positions persist across restart",()=>{
                    var p=new Preferences {Gpt=false,Deep=true,Interval=60,GptX=122,GptY=55};p.Save();var restored=Preferences.Load();Assert(!restored.Gpt&&restored.Deep&&restored.Interval==60&&restored.GptX==122);
                });
                check("Corrupt settings fall back without breaking launch",()=>{
                    File.WriteAllText(Paths.Preferences,"not json");Assert(Preferences.Load().Interval==30);
                });
            } finally {Paths.Root=saved;Directory.Delete(temp,true);}
            File.WriteAllText(report,Json.Write(new {passed=passed,failed=failed}));return failed.Count==0?0:1;
        }
        static void Assert(bool condition) {if(!condition)throw new Exception("Assertion failed");}
    }
}
