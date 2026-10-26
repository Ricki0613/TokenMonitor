using System;
using System.IO;
using System.Linq;

namespace TokenMonitor {
    internal static class HistoryUpdateTests {
        static void Assert(bool condition) {if(!condition)throw new Exception("History/update assertion failed");}
        public static void Run(Action<string,Action> check) {
            check("History buckets replace samples, keep sources separate and survive restart",()=>Temporary(dir=>{
                var at=DateTimeOffset.UtcNow;var store=new HistoryStore(Path.Combine(dir,"history.json"));
                store.Put("DeepSeek API CNY",at,100);store.Put("DeepSeek API CNY",at,90);store.Put("DeepSeek Harness CNY",at,125);
                store.Save(at);store=new HistoryStore(Path.Combine(dir,"history.json"));
                Assert(store.Points.Count==2&&store.Points[0].Value==90&&store.Points[1].Value==125);
            }));
            check("Daily usage is revised rather than added again; missing dates are absent",()=>Temporary(dir=>{
                var store=new HistoryStore(Path.Combine(dir,"history.json"));var at=DateTimeOffset.UtcNow;
                store.Put("OpenAI A",at,100,"2026-10-25");store.Put("OpenAI A",at.AddSeconds(30),80,"2026-10-25");store.Put("OpenAI B",at,7,"2026-10-25");
                Assert(store.Points.Count==2&&store.Points[0].Value==80);
            }));
            check("History prunes old samples and preserves corrupted files",()=>Temporary(dir=>{
                string path=Path.Combine(dir,"history.json");var store=new HistoryStore(path);var at=DateTimeOffset.UtcNow;
                store.Put("old",at.AddDays(-91),1);store.Put("current",at,2);store.Save(at);Assert(new HistoryStore(path).Points.Count==1);
                File.WriteAllText(path,"broken");bool failed=false;try{new HistoryStore(path);}catch{failed=true;}Assert(failed&&File.ReadAllText(path)=="broken");
            }));
            check("Release digest uses exact package name and never remote download URL",()=>{
                var release=UpdateClient.Parse(Json.Write(new {tag_name="v3.2.0",draft=false,prerelease=false,body="User-visible changes",assets=new[]{new {name="TokenMonitor-v3.2.0-windows-x64.zip",digest="sha256:"+new string('a',64),browser_download_url="https://example.com/evil"}}}));
                Assert(release.AssetHash==new string('a',64)&&release.AssetUrl==UpdateClient.ReleasesUrl+"/download/v3.2.0/TokenMonitor-v3.2.0-windows-x64.zip"&&release.Notes=="User-visible changes");
            });
            check("Update hash mismatch is rejected without modifying installed program",()=>Temporary(dir=>{
                var plan=Plan(dir);plan.Hashes[0]=new string('0',64);bool rejected=false;try{UpdateInstaller.Apply(plan);}catch(InvalidDataException){rejected=true;}Assert(rejected&&File.ReadAllText(Path.Combine(plan.Target,UpdateInstaller.Files[0]))=="old0");
            }));
            check("Updater replaces files, preserves data and retains a verified previous version",()=>Temporary(dir=>{
                var plan=Plan(dir);File.WriteAllText(Path.Combine(plan.Target,"settings.json"),"keep");UpdateInstaller.Apply(plan);
                for(int i=0;i<3;i++){Assert(File.ReadAllText(Path.Combine(plan.Target,UpdateInstaller.Files[i]))=="new"+i);Assert(File.ReadAllText(Path.Combine(plan.Target,"previous-version",UpdateInstaller.Files[i]))=="old"+i);}
                Assert(File.ReadAllText(Path.Combine(plan.Target,"settings.json"))=="keep");
                plan.Source=Path.Combine(plan.Target,"previous-version");plan.Backup=Path.Combine(dir,"rollback-backup");plan.Hashes=UpdateInstaller.Files.Select(n=>UpdateInstaller.Hash(Path.Combine(plan.Source,n))).ToArray();UpdateInstaller.Apply(plan);
                Assert(File.ReadAllText(Path.Combine(plan.Target,UpdateInstaller.Files[0]))=="old0");
            }));
            check("Failed replacement restores files already replaced",()=>Temporary(dir=>{
                var plan=Plan(dir);bool failed=false;
                using(var locked=new FileStream(Path.Combine(plan.Target,UpdateInstaller.Files[1]),FileMode.Open,FileAccess.Read,FileShare.Read)) {
                    try{UpdateInstaller.Apply(plan);}catch(IOException){failed=true;}
                    Assert(failed&&File.ReadAllText(Path.Combine(plan.Target,UpdateInstaller.Files[0]))=="old0");
                }
                Assert(File.ReadAllText(Path.Combine(plan.Target,UpdateInstaller.Files[2]))=="old2");
            }));
        }
        static UpdatePlan Plan(string dir) {
            var plan=new UpdatePlan {Target=Path.Combine(dir,"target"),Source=Path.Combine(dir,"source"),Backup=Path.Combine(dir,"backup")};Directory.CreateDirectory(plan.Target);Directory.CreateDirectory(plan.Source);
            for(int i=0;i<3;i++){File.WriteAllText(Path.Combine(plan.Target,UpdateInstaller.Files[i]),"old"+i);File.WriteAllText(Path.Combine(plan.Source,UpdateInstaller.Files[i]),"new"+i);}
            plan.Hashes=UpdateInstaller.Files.Select(n=>UpdateInstaller.Hash(Path.Combine(plan.Source,n))).ToArray();return plan;
        }
        static void Temporary(Action<string> action) {string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"history-update-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{action(dir);}finally{Directory.Delete(dir,true);}}
    }
}
