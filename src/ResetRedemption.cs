using System;
using System.Threading.Tasks;

namespace TokenMonitor {
    // Save before sending, retain the request ID on uncertain results, and reuse it across restarts.
    public sealed class ResetRedemption {
        readonly Preferences prefs;
        readonly Func<string,Task<ResetResult>> consume;
        readonly Action persist;
        public ResetRedemption(Preferences prefs,Func<string,Task<ResetResult>> consume,Action persist=null) {
            this.prefs=prefs;this.consume=consume;this.persist=persist??prefs.Save;
        }
        public async Task<ResetResult> Redeem() {
            if(string.IsNullOrEmpty(prefs.PendingResetKey)) {
                prefs.PendingResetKey=Guid.NewGuid().ToString();
                try {persist();}catch {prefs.PendingResetKey=null;throw;}
            }
            string key=prefs.PendingResetKey;
            var result=await consume(key);
            prefs.PendingResetKey=null;
            try {persist();}catch {prefs.PendingResetKey=key;throw;}
            return result;
        }
    }
}
