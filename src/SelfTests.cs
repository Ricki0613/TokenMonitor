using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;

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
            check("Reset credits preserve known zero and unknown, reject fractional counts",()=>{
                Assert(GptSnapshot.Parse(Json.Read("{\"rateLimits\":{},\"rateLimitResetCredits\":{\"availableCount\":0}}" )).ResetCredits==0);
                Assert(!GptSnapshot.Parse(Json.Read("{\"rateLimits\":{},\"rateLimitResetCredits\":{\"availableCount\":1.5}}" )).ResetCredits.HasValue);
                Assert(!GptSnapshot.Parse(Json.Read("{\"rateLimits\":{}}" )).ResetCredits.HasValue);
            });
            check("Official reset outcomes distinguish redeemed, ineligible and no-credit",()=>{
                foreach(var outcome in new[]{"reset","alreadyRedeemed","nothingToReset","noCredit"})
                    Assert(ResetResult.Parse(Json.Read("{\"outcome\":\""+outcome+"\"}")).Success==(outcome=="reset"||outcome=="alreadyRedeemed"));
                bool rejected=false;try{ResetResult.Parse(Json.Read("{}"));}catch(ProviderException){rejected=true;}Assert(rejected);
            });
            check("Failed preference save prevents any reset request",()=>{
                var p=new Preferences();int sent=0;bool failedSave=false;
                try {new ResetRedemption(p,key=>{sent++;return Task.FromResult(ResetResult.Parse(Json.Read("{\"outcome\":\"reset\"}")));},()=>{throw new IOException();}).Redeem().GetAwaiter().GetResult();}
                catch(IOException){failedSave=true;}
                Assert(failedSave&&sent==0&&p.PendingResetKey==null);
            });
            check("Completion save failure retains original idempotency key",()=>{
                var p=new Preferences();string sentKey=null;int saves=0;
                try {new ResetRedemption(p,key=>{sentKey=key;return Task.FromResult(ResetResult.Parse(Json.Read("{\"outcome\":\"reset\"}")));},()=>{if(++saves==2)throw new IOException();}).Redeem().GetAwaiter().GetResult();}catch(IOException){}
                Assert(sentKey!=null&&p.PendingResetKey==sentKey);
            });
            var saved=Paths.Root;var temp=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"test-data-"+Guid.NewGuid().ToString("N"));
            var savedData=Paths.TestDataDirectory;
            Directory.CreateDirectory(temp);Paths.Root=Path.Combine(temp,"program");Paths.TestDataDirectory=Path.Combine(temp,"user-data");
            try {
                check("DPAPI credential round-trip and no plaintext at rest",()=>{
                    string fake="sk-test-only-not-a-real-secret";SecretStore.Save(fake);Assert(SecretStore.Read()==fake);
                    Assert(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Paths.Key)).Contains(fake));
                });
                check("Visibility and positions persist across restart",()=>{
                    var p=new Preferences {Gpt=false,Deep=true,Interval=60,GptX=122,GptY=55};p.Save();var restored=Preferences.Load();Assert(!restored.Gpt&&restored.Deep&&restored.Interval==60&&restored.GptX==122);
                });
                check("Version 1.0 settings migrate without changing positions or visibility",()=>{
                    Directory.CreateDirectory(Paths.Data);File.WriteAllText(Paths.Preferences,"{\"Gpt\":false,\"Deep\":true,\"Interval\":60,\"GptX\":122,\"GptY\":55}");
                    var p=Preferences.Load();Assert(!p.Gpt&&p.Deep&&p.GptX==122&&p.Appearance=="system"&&p.GptWidth==368&&p.GptAutoHeight);
                });
                check("Appearance, resized geometry and auto-height mode persist",()=>{
                    var p=new Preferences {Appearance="dark",GptWidth=660,GptHeight=260,GptAutoHeight=false,DeepWidth=300,DeepAutoHeight=true};p.Save();var restored=Preferences.Load();
                    Assert(restored.Appearance=="dark"&&restored.GptWidth==660&&restored.GptHeight==260&&!restored.GptAutoHeight&&restored.DeepWidth==300&&restored.DeepAutoHeight);
                });
                check("Invalid appearance and dimensions recover to usable defaults",()=>{
                    File.WriteAllText(Paths.Preferences,"{\"Appearance\":\"bad\",\"GptWidth\":0,\"GptHeight\":-1,\"DeepWidth\":99999}");
                    var p=Preferences.Load();Assert(p.Appearance=="system"&&p.GptWidth==368&&p.GptHeight==500&&p.DeepWidth==1600);
                });
                check("Timed-out reset reuses persisted ID after restart and clears on success",()=>{
                    var p=new Preferences();string firstKey=null;
                    try {new ResetRedemption(p,key=>{firstKey=key;var source=new TaskCompletionSource<ResetResult>();source.SetException(new TimeoutException());return source.Task;}).Redeem().GetAwaiter().GetResult();}catch(TimeoutException){}
                    var restored=Preferences.Load();Assert(firstKey!=null&&restored.PendingResetKey==firstKey);
                    string secondKey=null;var result=new ResetRedemption(restored,key=>{secondKey=key;return Task.FromResult(ResetResult.Parse(Json.Read("{\"outcome\":\"alreadyRedeemed\"}")));}).Redeem().GetAwaiter().GetResult();
                    Assert(result.Success&&secondKey==firstKey&&Preferences.Load().PendingResetKey==null);
                });
                check("No-credit and ineligible results clear pending reset without success",()=>{
                    foreach(var outcome in new[]{"noCredit","nothingToReset"}) {
                        var p=new Preferences();var result=new ResetRedemption(p,key=>Task.FromResult(ResetResult.Parse(Json.Read("{\"outcome\":\""+outcome+"\"}")))).Redeem().GetAwaiter().GetResult();
                        Assert(!result.Success&&p.PendingResetKey==null&&Preferences.Load().PendingResetKey==null);
                    }
                });
                check("Corrupt settings fall back without breaking launch",()=>{
                    File.WriteAllText(Paths.Preferences,"not json");Assert(Preferences.Load().Interval==30);
                });
                check("Production storage uses Windows current-user LocalApplicationData",()=>{
                    var data=Paths.TestDataDirectory;
                    try {Paths.TestDataDirectory=null;Assert(Paths.Data==Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TokenMonitor"));}
                    finally {Paths.TestDataDirectory=data;}
                });
                check("Fresh installation automatically creates user data without an install data folder",()=>StorageCase(temp,"fresh",()=>{
                    LocalStorage.Initialize();new Preferences {Appearance="dark"}.Save();SecretStore.Save("sk-test-only-fresh-install");
                    Assert(Preferences.Load().Appearance=="dark"&&SecretStore.Read()=="sk-test-only-fresh-install");
                    Assert(Directory.GetFileSystemEntries(Paths.Root).Length==0);
                }));
                check("Old settings, encrypted key and pending reset migrate without deleting originals",()=>StorageCase(temp,"migration",()=>{
                    string target=Paths.TestDataDirectory,legacy=Path.Combine(Paths.Root,"data"),pending=Guid.NewGuid().ToString();
                    Paths.TestDataDirectory=legacy;new Preferences {Appearance="dark",Gpt=false,GptWidth=660,PendingResetKey=pending}.Save();SecretStore.Save("sk-test-only-legacy-key");
                    byte[] oldKey=File.ReadAllBytes(Paths.Key),oldPrefs=File.ReadAllBytes(Paths.Preferences);
                    Paths.TestDataDirectory=target;Assert(LocalStorage.Migrate(legacy)==null);
                    var restored=Preferences.Load();Assert(restored.Appearance=="dark"&&!restored.Gpt&&restored.GptWidth==660&&restored.PendingResetKey==pending);
                    Assert(SecretStore.Read()=="sk-test-only-legacy-key"&&File.ReadAllBytes(Path.Combine(legacy,"deepseek.key")).SequenceEqual(oldKey)&&File.ReadAllBytes(Path.Combine(legacy,"settings.json")).SequenceEqual(oldPrefs));
                    Assert(LocalStorage.Migrate(legacy)==null&&File.ReadAllBytes(Paths.Key).SequenceEqual(oldKey));
                }));
                check("Existing user preferences win while missing legacy key is imported",()=>StorageCase(temp,"partial",()=>{
                    string target=Paths.TestDataDirectory,legacy=Path.Combine(Paths.Root,"data");
                    Paths.TestDataDirectory=legacy;new Preferences {Appearance="dark"}.Save();SecretStore.Save("sk-test-only-legacy-key");
                    Paths.TestDataDirectory=target;new Preferences {Appearance="light"}.Save();LocalStorage.Migrate(legacy);
                    Assert(Preferences.Load().Appearance=="light"&&SecretStore.Read()=="sk-test-only-legacy-key");
                    SecretStore.Save("sk-test-only-new-key");LocalStorage.Migrate(legacy);Assert(SecretStore.Read()=="sk-test-only-new-key");
                }));
                check("Undecryptable old key is preserved and produces actionable notice",()=>StorageCase(temp,"bad-key",()=>{
                    string legacy=Path.Combine(Paths.Root,"data");Directory.CreateDirectory(legacy);File.WriteAllBytes(Path.Combine(legacy,"deepseek.key"),new byte[]{1,2,3});
                    string notice=LocalStorage.Migrate(legacy);Assert(notice.Contains("重新输入")&&!File.Exists(Paths.Key)&&File.Exists(Path.Combine(legacy,"deepseek.key")));
                }));
                check("Malformed old preferences are not silently imported",()=>StorageCase(temp,"bad-prefs",()=>{
                    string legacy=Path.Combine(Paths.Root,"data");Directory.CreateDirectory(legacy);File.WriteAllText(Path.Combine(legacy,"settings.json"),"not json");
                    string notice=LocalStorage.Migrate(legacy);Assert(notice.Contains("格式无效")&&!File.Exists(Paths.Preferences));
                }));
                check("Saving works when Windows ACL denies writes to the program folder",()=>StorageCase(temp,"read-only-program",()=>{
                    var directory=new DirectoryInfo(Paths.Root);var original=directory.GetAccessControl();var denied=directory.GetAccessControl();
                    denied.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User,FileSystemRights.Write,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Deny));
                    try {
                        directory.SetAccessControl(denied);bool blocked=false;
                        try {File.WriteAllText(Path.Combine(Paths.Root,"write-probe.txt"),"test");}catch(UnauthorizedAccessException){blocked=true;}
                        Assert(blocked);LocalStorage.Initialize();new Preferences {Appearance="dark"}.Save();SecretStore.Save("sk-test-only-readonly-program");
                        Assert(Preferences.Load().Appearance=="dark"&&SecretStore.Read()=="sk-test-only-readonly-program");
                        new Preferences {Appearance="light"}.Save();Assert(Preferences.Load().Appearance=="light");
                    } finally {directory.SetAccessControl(original);}
                }));
                check("Atomic write failure preserves previous file and cleans temporary ciphertext",()=>StorageCase(temp,"atomic-failure",()=>{
                    LocalStorage.Initialize();SecretStore.Save("sk-test-only-existing-key");byte[] previous=File.ReadAllBytes(Paths.Key);
                    File.SetAttributes(Paths.Key,FileAttributes.ReadOnly);bool failedWrite=false;
                    try {SecretStore.Save("sk-test-only-replacement-key");}catch(UnauthorizedAccessException){failedWrite=true;}
                    finally {File.SetAttributes(Paths.Key,FileAttributes.Normal);}
                    Assert(failedWrite&&File.ReadAllBytes(Paths.Key).SequenceEqual(previous)&&Directory.GetFiles(Paths.Data,"*.tmp").Length==0);
                }));
                check("Storage diagnostics distinguish permission, encryption and IO without raw secrets",()=>{
                    const string sensitive="sk-do-not-include-this-in-diagnostics";
                    Assert(LocalStorage.Explain(new UnauthorizedAccessException(sensitive),"保存设置").Contains("访问被拒绝"));
                    Assert(LocalStorage.Explain(new System.Security.Cryptography.CryptographicException(sensitive),"保存密钥").Contains("加密失败"));
                    var message=LocalStorage.Explain(new IOException(sensitive),"保存设置");Assert(message.Contains("磁盘空间")&&!message.Contains(sensitive)&&message.Contains("0x"));
                });
            } finally {Paths.Root=saved;Paths.TestDataDirectory=savedData;Directory.Delete(temp,true);}
            File.WriteAllText(report,Json.Write(new {passed=passed,failed=failed}));return failed.Count==0?0:1;
        }
        static void Assert(bool condition) {if(!condition)throw new Exception("Assertion failed");}
        static void StorageCase(string root,string name,Action action) {
            string savedRoot=Paths.Root,savedData=Paths.TestDataDirectory;
            try {Paths.Root=Path.Combine(root,name,"program");Paths.TestDataDirectory=Path.Combine(root,name,"user-data");Directory.CreateDirectory(Paths.Root);action();}
            finally {Paths.Root=savedRoot;Paths.TestDataDirectory=savedData;LocalStorage.Notice=null;}
        }
    }
}
