using System;
using System.IO;
namespace TokenMonitor {
    public static class DeepSeekHarnessTests {
        public static void Run(Action<string,Action> check) {
            string grant="version: 1\nrecords:\n  deepseek-account-platform/default:\n    kind: grant\n    payload:\n      version: 1\n      token: test-token\n      issuer: https://platform.deepseek.com\n";
            check("Harness reads only account grant, not browser session",()=>{
                Require(DeepSeekHarness.ParseToken(grant+"  client-connection/browser-session:\n    kind: secret\n    payload:\n      token: wrong-token\n")=="test-token");
            });
            check("Harness supports quoted YAML scalars",()=>Require(DeepSeekHarness.ParseToken(grant.Replace("test-token","'test-token'").Replace("grant","\"grant\""))=="test-token"));
            check("Harness rejects foreign issuer and malformed grants without disclosing secrets",()=>{
                foreach(var bad in new[]{grant.Replace("https://platform.deepseek.com","https://example.com"),grant.Replace("version: 1","version: 2"),grant.Replace("kind: grant","kind: secret"),grant.Replace("token: test-token","token: |"),grant.Replace("      issuer:","      token: other-secret\n      issuer:"),"records: {}"}) {
                    bool rejected=false;try {DeepSeekHarness.ParseToken(bad);}catch(ProviderException e){rejected=!e.Message.Contains("test-token")&&!e.Message.Contains("other-secret");}Require(rejected);
                }
            });
            string summary="{\"code\":0,\"data\":{\"biz_code\":0,\"biz_data\":{\"normal_wallets\":[{\"currency\":\"CNY\",\"balance\":\"1.2345\"},{\"currency\":\"USD\",\"balance\":\"-0.1\"}],\"bonus_wallets\":[{\"currency\":\"CNY\",\"balance\":\"6e0\"}]}}}";
            check("Harness merges wallet types by currency and preserves decimal precision",()=>{
                var s=DeepSeekHarness.ParseBalance(Json.Read(summary));Require(s.FromHarness&&s.Balances.Count==2&&s.Balances[0].Total==7.2345m&&s.Balances[0].ToppedUp==1.2345m&&s.Balances[0].Granted==6m&&s.Balances[1].Total==-0.1m);
            });
            check("Harness missing or malformed wallets never become zero",()=>{
                foreach(var bad in new[]{summary.Replace("bonus_wallets","missing"),summary.Replace("1.2345","NaN"),summary.Replace("USD","EUR"),"{\"code\":0,\"data\":{\"biz_code\":0,\"biz_data\":{\"normal_wallets\":[],\"bonus_wallets\":[]}}}"}) {
                    bool rejected=false;try {DeepSeekHarness.ParseBalance(Json.Read(bad));}catch(InvalidDataException){rejected=true;}Require(rejected);
                }
            });
            check("Harness expired login has actionable error",()=>{
                bool rejected=false;try{DeepSeekHarness.ParseBalance(Json.Read("{\"code\":40003}"));}catch(ProviderException e){rejected=e.Message.Contains("重新登录");}Require(rejected);
            });
            check("Older preferences keep manual API source",()=>Require(new Preferences().DeepSource=="api"));
        }
        static void Require(bool ok) {if(!ok)throw new Exception("Assertion failed");}
    }
}
