using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TokenMonitor {
    public sealed class OpenAiConnection {
        public string ApiKey, AdminKey, ApiKeyId;
    }

    public sealed class OpenAiTokenTotals {
        public long InputTokens, OutputTokens, Requests;
        public long? CachedInputTokens=0;
        // Cached input is already included in input_tokens by the official API.
        public long TotalTokens { get { return checked(InputTokens + OutputTokens); } }
        internal void Add(OpenAiTokenTotals other) {
            try {
                InputTokens=checked(InputTokens+other.InputTokens);
                OutputTokens=checked(OutputTokens+other.OutputTokens);
                CachedInputTokens=CachedInputTokens.HasValue&&other.CachedInputTokens.HasValue
                    ?(long?)checked(CachedInputTokens.Value+other.CachedInputTokens.Value):null;
                Requests=checked(Requests+other.Requests);
                if(InputTokens>long.MaxValue-OutputTokens) throw OpenAiSnapshot.Invalid();
            } catch(OverflowException) { throw OpenAiSnapshot.Invalid(); }
        }
    }

    public sealed class OpenAiSnapshot {
        public DateTimeOffset Fetched, Start, End;
        public string ApiKeyId;
        public OpenAiTokenTotals Today=new OpenAiTokenTotals(), Month=new OpenAiTokenTotals();
        public static readonly TimeSpan BeijingOffset=TimeSpan.FromHours(8);

        public static DateTimeOffset DayStart(DateTimeOffset at) {
            var local=at.ToOffset(BeijingOffset);
            return new DateTimeOffset(local.Year,local.Month,local.Day,0,0,0,BeijingOffset).ToUniversalTime();
        }
        public static DateTimeOffset MonthStart(DateTimeOffset at) {
            var local=at.ToOffset(BeijingOffset);
            return new DateTimeOffset(local.Year,local.Month,1,0,0,0,BeijingOffset).ToUniversalTime();
        }

        // This accepts complete official pages so the aggregation can be checked offline.
        // Queries use hourly buckets; Beijing midnight is an exact UTC hour boundary.
        public static OpenAiSnapshot Parse(IEnumerable<Dictionary<string,object>> pages,string apiKeyId,DateTimeOffset at) {
            apiKeyId=OpenAiProvider.RequireKeyId(apiKeyId);
            if(pages==null) throw Invalid();
            var snapshot=new OpenAiSnapshot {
                ApiKeyId=apiKeyId, Fetched=at.ToUniversalTime(), Start=MonthStart(at),
                End=DateTimeOffset.FromUnixTimeSeconds(at.ToUnixTimeSeconds())
            };
            long start=snapshot.Start.ToUnixTimeSeconds(), end=snapshot.End.ToUnixTimeSeconds(), today=DayStart(at).ToUnixTimeSeconds();
            bool first=true, expectingMore=true;
            var cursors=new HashSet<string>(StringComparer.Ordinal);
            var intervals=new List<long[]>();
            foreach(var page in pages) {
                if(!first&&!expectingMore) throw Invalid();
                first=false;
                var parsed=OpenAiUsagePage.Parse(page);
                expectingMore=parsed.HasMore;
                if(parsed.HasMore&&!cursors.Add(parsed.NextPage)) throw Invalid();
                foreach(var item in parsed.Buckets) {
                    var bucket=Json.Object(item);
                    if(bucket==null||Json.Str(bucket,"object")!="bucket") throw Invalid();
                    long bucketStart=Count(bucket,"start_time"), bucketEnd=Count(bucket,"end_time");
                    if(bucketEnd<=bucketStart||bucketEnd-bucketStart>3600||bucketStart<start||bucketStart>=end||
                       bucketEnd>end+3600||bucketStart<today&&bucketEnd>today) throw Invalid();
                    intervals.Add(new[]{bucketStart,bucketEnd});
                    foreach(var raw in Items(bucket,"results")) {
                        var result=Json.Object(raw);
                        if(result==null||Json.Str(result,"object")!="organization.usage.completions.result") throw Invalid();
                        // Never substitute ungrouped organization totals or another key's usage.
                        if(Json.Str(result,"api_key_id")!=apiKeyId)
                            throw new InvalidDataException("OpenAI 未返回指定 API Key ID 的分组用量，请检查密钥 ID 与管理权限");
                        var totals=new OpenAiTokenTotals {
                            InputTokens=Count(result,"input_tokens"), OutputTokens=Count(result,"output_tokens"),
                            CachedInputTokens=Json.Get(result,"input_cached_tokens")==null?(long?)null:Count(result,"input_cached_tokens"),
                            Requests=Count(result,"num_model_requests")
                        };
                        if(totals.CachedInputTokens.HasValue&&totals.CachedInputTokens.Value>totals.InputTokens) throw Invalid();
                        snapshot.Month.Add(totals);
                        if(bucketStart>=today) snapshot.Today.Add(totals);
                    }
                }
            }
            if(first||expectingMore) throw new InvalidDataException("OpenAI 用量分页未完整返回，请稍后重试");
            intervals.Sort((a,b)=>a[0].CompareTo(b[0]));
            for(int i=1;i<intervals.Count;i++)
                if(intervals[i][0]<intervals[i-1][1]) throw new InvalidDataException("OpenAI 用量时间桶重复或重叠，请稍后重试");
            return snapshot;
        }

        internal static InvalidDataException Invalid() {
            return new InvalidDataException("OpenAI 官方用量格式不完整或发生变化，请稍后重试");
        }
        internal static IEnumerable Items(Dictionary<string,object> obj,string key) {
            var value=Json.Get(obj,key);
            var items=value as IEnumerable;
            if(items==null||value is string||value is IDictionary) throw Invalid();
            return items;
        }
        internal static long Count(Dictionary<string,object> obj,string key) {
            object value=Json.Get(obj,key);
            if(!(value is byte||value is short||value is int||value is long||value is float||value is double||value is decimal)) throw Invalid();
            if(value is float||value is double) {
                double floating=Convert.ToDouble(value,CultureInfo.InvariantCulture);
                if(double.IsNaN(floating)||double.IsInfinity(floating)||floating!=Math.Truncate(floating)) throw Invalid();
            }
            try {
                decimal number=Convert.ToDecimal(value,CultureInfo.InvariantCulture);
                if(number<0||number>long.MaxValue||number!=decimal.Truncate(number)) throw Invalid();
                return (long)number;
            } catch(OverflowException) { throw Invalid(); }
        }
    }

    internal sealed class OpenAiUsagePage {
        public IEnumerable Buckets;
        public bool HasMore;
        public string NextPage;
        public static OpenAiUsagePage Parse(Dictionary<string,object> obj) {
            if(obj==null||Json.Str(obj,"object")!="page"||!(Json.Get(obj,"has_more") is bool)) throw OpenAiSnapshot.Invalid();
            var page=new OpenAiUsagePage {
                Buckets=OpenAiSnapshot.Items(obj,"data"), HasMore=(bool)Json.Get(obj,"has_more"),
                NextPage=Json.Str(obj,"next_page")
            };
            if(page.HasMore&&(string.IsNullOrWhiteSpace(page.NextPage)||page.NextPage.Length>8192)) throw OpenAiSnapshot.Invalid();
            return page;
        }
    }

    public sealed class OpenAiProvider : IDisposable {
        const string BaseUrl="https://api.openai.com/v1/";
        readonly HttpClient http;
        readonly Func<DateTimeOffset> clock;
        public OpenAiProvider():this(new HttpClientHandler { AllowAutoRedirect=false },()=>DateTimeOffset.UtcNow) {}
        internal OpenAiProvider(HttpMessageHandler handler):this(handler,()=>DateTimeOffset.UtcNow) {}
        internal OpenAiProvider(HttpMessageHandler handler,Func<DateTimeOffset> clock) {
            if(handler==null||clock==null) throw new ArgumentNullException();
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            this.clock=clock;
            http=new HttpClient(handler);
            http.Timeout=TimeSpan.FromSeconds(20);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TokenMonitor/2.0.0");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            http.MaxResponseContentBufferSize=2097152;
        }

        // A models-list GET checks the supplied key without creating an inference request.
        public async Task ValidateApiKey(string apiKey) {
            apiKey=RequireSecret(apiKey,false);
            var result=await Get(BaseUrl+"models",apiKey,false).ConfigureAwait(false);
            if(Json.Str(result,"object")!="list") throw new ProviderException("OpenAI 未返回有效的模型列表，无法确认 API Key 登录");
            try { OpenAiSnapshot.Items(result,"data"); }
            catch(InvalidDataException) { throw new ProviderException("OpenAI 未返回有效的模型列表，无法确认 API Key 登录"); }
        }

        public async Task<OpenAiSnapshot> Fetch(OpenAiConnection connection) {
            if(connection==null||string.IsNullOrWhiteSpace(connection.ApiKey))
                throw new ProviderException("请先在设置中登录 OpenAI API Key");
            RequireSecret(connection.ApiKey,false);
            if(string.IsNullOrWhiteSpace(connection.AdminKey))
                throw new ProviderException("API Key 已保存；读取历史用量还需要组织 Admin API Key，请在设置中补充",300);
            if(string.IsNullOrWhiteSpace(connection.ApiKeyId))
                throw new ProviderException("请在设置中填写要监视的 API Key ID（key_…，不是 sk-… 密钥）",300);
            string adminKey=RequireSecret(connection.AdminKey,true), apiKeyId=RequireKeyId(connection.ApiKeyId);
            DateTimeOffset at=clock().ToUniversalTime(), start=OpenAiSnapshot.MonthStart(at);
            // end_time is exclusive Unix seconds. At the exact beginning of a month,
            // the elapsed interval is empty and must not be sent as an invalid query.
            if(at.ToUnixTimeSeconds()<=start.ToUnixTimeSeconds())
                return new OpenAiSnapshot { ApiKeyId=apiKeyId,Start=start,End=start,Fetched=clock().ToUniversalTime() };
            var pages=new List<Dictionary<string,object>>();
            var seen=new HashSet<string>(StringComparer.Ordinal);
            string cursor=null;
            do {
                var result=await Get(BuildUsageUri(start,at,apiKeyId,cursor),adminKey,true).ConfigureAwait(false);
                var page=OpenAiUsagePage.Parse(result);
                pages.Add(result);
                if(!page.HasMore) break;
                if(!seen.Add(page.NextPage)||pages.Count>=32) throw new ProviderException("OpenAI 用量分页异常，请稍后重试",60);
                cursor=page.NextPage;
            } while(true);
            var snapshot=OpenAiSnapshot.Parse(pages,apiKeyId,at);
            snapshot.Fetched=clock().ToUniversalTime();
            return snapshot;
        }

        internal static string BuildUsageUri(DateTimeOffset start,DateTimeOffset end,string apiKeyId,string page) {
            var url=BaseUrl+"organization/usage/completions?start_time="+start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)+
                "&end_time="+end.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)+
                "&bucket_width=1h&limit=168&api_key_ids="+Uri.EscapeDataString(RequireKeyId(apiKeyId))+"&group_by=api_key_id";
            return page==null?url:url+"&page="+Uri.EscapeDataString(page);
        }
        internal static string RequireKeyId(string keyId) {
            keyId=(keyId??"").Trim();
            if(keyId.Length<5||keyId.Length>200||!keyId.StartsWith("key_",StringComparison.Ordinal))
                throw new ProviderException("请输入 API Key ID（key_…）；不要把 sk-… 密钥填入 ID",300);
            foreach(char c in keyId)
                if(!(c>='A'&&c<='Z'||c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'||c=='-'))
                    throw new ProviderException("API Key ID 格式无效，请从官方项目密钥列表复制 ID",300);
            return keyId;
        }
        internal static string RequireSecret(string secret,bool admin) {
            secret=(secret??"").Trim();
            string label=admin?"组织 Admin API Key":"OpenAI API Key";
            if(secret.Length<16||secret.Length>4096||!secret.StartsWith(admin?"sk-admin-":"sk-",StringComparison.Ordinal)||
               !admin&&secret.StartsWith("sk-admin-",StringComparison.Ordinal))
                throw new ProviderException("请输入有效的 "+label,300);
            foreach(char c in secret)
                if(!(c>='A'&&c<='Z'||c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'||c=='-'))
                    throw new ProviderException(label+" 包含无效字符",300);
            return secret;
        }

        async Task<Dictionary<string,object>> Get(string uri,string secret,bool admin) {
            try {
                using(var request=new HttpRequestMessage(HttpMethod.Get,uri)) {
                    request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",secret);
                    using(var response=await http.SendAsync(request).ConfigureAwait(false)) {
                        int code=(int)response.StatusCode;
                        if(code==401) throw new ProviderException(admin?"OpenAI Admin API Key 无效或已过期，请在设置中更新":"OpenAI API Key 无效或已过期，请在设置中更新",300);
                        if(code==403) throw new ProviderException(admin?"此密钥没有组织用量读取权限，请使用对应组织的 Admin API Key":"此 API Key 无法读取模型列表，请检查密钥权限和组织访问权限",300);
                        if(code==429) {
                            int cooldown=60;
                            var retry=response.Headers.RetryAfter;
                            if(retry!=null) {
                                double seconds=retry.Delta.HasValue?retry.Delta.Value.TotalSeconds:
                                    retry.Date.HasValue?(retry.Date.Value-clock()).TotalSeconds:60;
                                cooldown=(int)Math.Min(3600,Math.Max(60,Math.Ceiling(seconds)));
                            }
                            throw new ProviderException("OpenAI 查询频率受限，稍后自动重试",cooldown);
                        }
                        if(code>=300&&code<400) throw new ProviderException("OpenAI 返回了重定向，为保护密钥已停止查询",60);
                        if(!response.IsSuccessStatusCode) throw new ProviderException("OpenAI 官方服务暂不可用（HTTP "+code+"）",60);
                        string content=await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        try { return Json.Read(content); }
                        catch { throw new ProviderException("OpenAI 官方返回格式异常，请稍后重试",60); }
                    }
                }
            } catch(HttpRequestException) { throw new ProviderException("无法连接 OpenAI 官方接口，请检查网络后重试",60); }
            catch(TaskCanceledException) { throw new ProviderException("OpenAI 用量查询超时，稍后自动重试",60); }
        }
        public void Dispose() { http.Dispose(); }
    }
}
