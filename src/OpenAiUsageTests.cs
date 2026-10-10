using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitor {
    internal static class OpenAiUsageTests {
        const string KeyId="key_test_selected";
        const string ApiKey="sk-test-only-openai-not-a-real-secret";
        const string AdminKey="sk-admin-test-only-not-a-real-secret";
        static readonly DateTimeOffset At=new DateTimeOffset(2026,10,10,1,30,0,TimeSpan.Zero);

        public static void Run(Action<string,Action> check) {
            check("OpenAI totals include input/output once and cache is a subset",()=>{
                var day=OpenAiSnapshot.DayStart(At);
                var snapshot=Parse(Page(false,null,
                    Bucket(day.AddHours(-1),Result(100,20,50,2)),
                    Bucket(day,Result(300,70,100,3))));
                Assert(snapshot.Month.InputTokens==400&&snapshot.Month.OutputTokens==90&&snapshot.Month.TotalTokens==490);
                Assert(snapshot.Month.CachedInputTokens==150&&snapshot.Month.Requests==5);
                Assert(snapshot.Today.TotalTokens==370&&snapshot.Today.CachedInputTokens==100&&snapshot.Today.Requests==3);
            });
            check("OpenAI Beijing day and month boundaries are independent of PC timezone",()=>{
                var at=new DateTimeOffset(2026,10,1,0,30,0,TimeSpan.FromHours(8));
                var expected=new DateTimeOffset(2026,9,30,16,0,0,TimeSpan.Zero);
                Assert(OpenAiSnapshot.DayStart(at)==expected&&OpenAiSnapshot.MonthStart(at)==expected);
                var snapshot=OpenAiSnapshot.Parse(new[]{Page(false,null,Bucket(expected,Result(9,1,2,1)))},KeyId,at);
                Assert(snapshot.Start==expected&&snapshot.Start.Offset==TimeSpan.Zero&&snapshot.End==at.ToUniversalTime());
                Assert(snapshot.Today.TotalTokens==10&&snapshot.Month.TotalTokens==10);
                var midnight=new DateTimeOffset(2026,10,10,0,30,0,TimeSpan.FromHours(8));
                var day=OpenAiSnapshot.DayStart(midnight);
                snapshot=OpenAiSnapshot.Parse(new[]{Page(false,null,
                    Bucket(day.AddHours(-1),Result(40,10,0,1)),Bucket(day,Result(50,20,10,1)))},KeyId,midnight);
                Assert(snapshot.Today.TotalTokens==70&&snapshot.Month.TotalTokens==120);
            });
            check("OpenAI missing optional cached tokens stays unknown rather than zero",()=>{
                var day=OpenAiSnapshot.DayStart(At);
                var result=Result(10,3,0,1);result.Remove("input_cached_tokens");
                var snapshot=Parse(Page(false,null,Bucket(day.AddHours(-1),Result(2,1,1,1)),Bucket(day,result)));
                Assert(snapshot.Month.TotalTokens==16&&snapshot.Today.TotalTokens==13);
                Assert(!snapshot.Month.CachedInputTokens.HasValue&&!snapshot.Today.CachedInputTokens.HasValue);
            });
            check("OpenAI authenticated empty usage is a valid zero response",()=>{
                var empty=Parse(Page(false,null));
                Assert(empty.Month.TotalTokens==0&&empty.Today.TotalTokens==0&&empty.Month.CachedInputTokens==0);
                var bucket=Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At))));
                Assert(bucket.Month.Requests==0&&bucket.Today.TotalTokens==0);
            });
            check("OpenAI exact Beijing month start is an empty interval without HTTP",()=>{
                var midnight=new DateTimeOffset(2026,10,1,0,0,0,TimeSpan.FromHours(8)).AddMilliseconds(500);
                var handler=new MockHandler((request,index)=>{throw new Exception("unexpected empty-interval query");});
                using(var provider=new OpenAiProvider(handler,()=>midnight)) {
                    var snapshot=provider.Fetch(Connection()).GetAwaiter().GetResult();
                    Assert(snapshot.Start==snapshot.End&&snapshot.Month.TotalTokens==0&&snapshot.Today.TotalTokens==0);
                }
                Assert(handler.Requests.Count==0);
            });
            check("OpenAI rejects missing, fractional, negative and overflowing required counters",()=>{
                foreach(string field in new[]{"input_tokens","output_tokens","num_model_requests"}) {
                    var result=Result(10,2,1,1);result.Remove(field);
                    Reject(()=>Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At),result))));
                }
                foreach(object value in new object[]{-1,1.5,1.0000000000000002,double.Epsilon,"10",double.NaN}) {
                    var result=Result(10,2,1,1);result["input_tokens"]=value;
                    Reject(()=>Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At),result))));
                }
                Reject(()=>Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At),Result(long.MaxValue,1,0,1)))));
                Reject(()=>Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At),Result(1,1,2,1)))));
            });
            check("OpenAI ungrouped and other-key usage is never treated as selected-key totals",()=>{
                foreach(string id in new[]{null,"key_another"}) {
                    var result=Result(10,2,1,1);result["api_key_id"]=id;
                    Reject(()=>Parse(Page(false,null,Bucket(OpenAiSnapshot.DayStart(At),result))));
                }
            });
            check("OpenAI incomplete, repeated and overlapping pages are rejected",()=>{
                var day=OpenAiSnapshot.DayStart(At);
                Reject(()=>Parse(Page(true,"next",Bucket(day,Result(1,1,0,1)))));
                Reject(()=>Parse(Page(true,null)));
                Reject(()=>OpenAiSnapshot.Parse(new[]{Page(true,"next"),Page(true,"next")},KeyId,At));
                Reject(()=>OpenAiSnapshot.Parse(new[]{Page(false,null),Page(false,null)},KeyId,At));
                Reject(()=>Parse(Page(false,null,Bucket(day,Result(1,1,0,1)),Bucket(day,Result(2,1,0,1)))));
                var malformed=Page(false,null);malformed.Remove("data");Reject(()=>Parse(malformed));
                Reject(()=>OpenAiSnapshot.Parse(new Dictionary<string,object>[0],KeyId,At));
            });
            check("OpenAI models login uses only the ordinary key with a read-only GET",()=>{
                var handler=new MockHandler((request,index)=>Response(HttpStatusCode.OK,"{\"object\":\"list\",\"data\":[]}"));
                using(var provider=new OpenAiProvider(handler,()=>At)) provider.ValidateApiKey(ApiKey).GetAwaiter().GetResult();
                Assert(handler.Requests.Count==1&&handler.Requests[0].Method=="GET");
                Assert(handler.Requests[0].Uri=="https://api.openai.com/v1/models"&&handler.Requests[0].Authorization=="Bearer "+ApiKey);
                Assert(!handler.Requests[0].HasContent);
            });
            check("OpenAI queries the exact selected ID with admin auth and follows all page cursors",()=>{
                var day=OpenAiSnapshot.DayStart(At);
                var handler=new MockHandler((request,index)=>Response(HttpStatusCode.OK,Json.Write(index==0
                    ?Page(true,"cursor +/=",Bucket(day,Result(10,2,3,1)))
                    :Page(false,null,Bucket(day.AddHours(1),Result(20,4,5,2))))));
                OpenAiSnapshot snapshot;
                using(var provider=new OpenAiProvider(handler,()=>At)) snapshot=provider.Fetch(Connection()).GetAwaiter().GetResult();
                Assert(snapshot.Month.TotalTokens==36&&snapshot.Month.Requests==3&&handler.Requests.Count==2);
                foreach(var request in handler.Requests) {
                    var uri=new Uri(request.Uri);
                    var query=Query(uri.Query);
                    Assert(request.Method=="GET"&&!request.HasContent&&request.Authorization=="Bearer "+AdminKey);
                    Assert(uri.Host=="api.openai.com"&&uri.AbsolutePath=="/v1/organization/usage/completions");
                    Assert(query["api_key_ids"]==KeyId&&query["group_by"]=="api_key_id"&&query["bucket_width"]=="1h"&&query["limit"]=="168");
                    Assert(query["start_time"]==OpenAiSnapshot.MonthStart(At).ToUnixTimeSeconds().ToString());
                    Assert(query["end_time"]==At.ToUnixTimeSeconds().ToString());
                }
                Assert(Query(new Uri(handler.Requests[1].Uri).Query)["page"]=="cursor +/=");
                Assert(!handler.Requests[0].Uri.Contains(ApiKey)&&!handler.Requests[0].Uri.Contains(AdminKey));
            });
            check("OpenAI fails the whole query when a later page fails",()=>{
                var handler=new MockHandler((request,index)=>index==0
                    ?Response(HttpStatusCode.OK,Json.Write(Page(true,"next",Bucket(OpenAiSnapshot.DayStart(At),Result(10,2,1,1)))))
                    :Response(HttpStatusCode.InternalServerError,"{\"error\":{\"message\":\"secret-server-detail\"}}"));
                using(var provider=new OpenAiProvider(handler,()=>At)) {
                    var error=ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult());
                    Assert(error.Message.Contains("HTTP 500")&&!error.Message.Contains("secret-server-detail"));
                }
                Assert(handler.Requests.Count==2);
            });
            check("OpenAI login-only or missing target ID makes no usage HTTP request",()=>{
                var handler=new MockHandler((request,index)=>{throw new Exception("unexpected HTTP request");});
                using(var provider=new OpenAiProvider(handler,()=>At)) {
                    ProviderFailure(()=>provider.Fetch(new OpenAiConnection { ApiKey=ApiKey }).GetAwaiter().GetResult());
                    ProviderFailure(()=>provider.Fetch(new OpenAiConnection { ApiKey=ApiKey,AdminKey=AdminKey }).GetAwaiter().GetResult());
                    ProviderFailure(()=>provider.Fetch(new OpenAiConnection { ApiKey=ApiKey,AdminKey=AdminKey,ApiKeyId=ApiKey }).GetAwaiter().GetResult());
                    ProviderFailure(()=>provider.ValidateApiKey(AdminKey).GetAwaiter().GetResult());
                }
                Assert(handler.Requests.Count==0);
            });
            check("OpenAI 401 and 403 distinguish invalid key from missing admin permissions",()=>{
                foreach(var code in new[]{HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden}) {
                    var handler=new MockHandler((request,index)=>Response(code,"{\"error\":{\"message\":\"private-detail\"}}"));
                    using(var provider=new OpenAiProvider(handler,()=>At)) {
                        var error=ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult());
                        Assert(error.Cooldown==300&&!error.Message.Contains("private-detail")&&!error.Message.Contains(AdminKey));
                        Assert(code==HttpStatusCode.Unauthorized?error.Message.Contains("无效"):error.Message.Contains("读取权限"));
                    }
                }
            });
            check("OpenAI respects bounded Retry-After deltas and dates",()=>{
                foreach(int seconds in new[]{5,121,7200}) {
                    var handler=new MockHandler((request,index)=>{
                        var response=Response((HttpStatusCode)429,"{}");
                        response.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));return response;
                    });
                    using(var provider=new OpenAiProvider(handler,()=>At))
                        Assert(ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult()).Cooldown==Math.Max(60,Math.Min(3600,seconds)));
                }
                var dated=new MockHandler((request,index)=>{
                    var response=Response((HttpStatusCode)429,"{}");response.Headers.RetryAfter=new RetryConditionHeaderValue(At.AddSeconds(180));return response;
                });
                using(var provider=new OpenAiProvider(dated,()=>At))
                    Assert(ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult()).Cooldown==180);
            });
            check("OpenAI redirect response stops instead of forwarding a credential",()=>{
                var handler=new MockHandler((request,index)=>{
                    var response=Response(HttpStatusCode.Redirect,"{}");
                    response.Headers.Location=new Uri("https://example.invalid/credential-trap");return response;
                });
                using(var provider=new OpenAiProvider(handler,()=>At))
                    Assert(ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult()).Message.Contains("重定向"));
                Assert(handler.Requests.Count==1&&new Uri(handler.Requests[0].Uri).Host=="api.openai.com");
            });
            check("OpenAI timeout and network failures expose no raw credentials",()=>{
                foreach(Exception failure in new Exception[]{new TaskCanceledException(ApiKey),new HttpRequestException(AdminKey)}) {
                    var handler=new MockHandler((request,index)=>{throw failure;});
                    using(var provider=new OpenAiProvider(handler,()=>At)) {
                        var error=ProviderFailure(()=>provider.Fetch(Connection()).GetAwaiter().GetResult());
                        Assert(error.Cooldown==60&&!error.Message.Contains(ApiKey)&&!error.Message.Contains(AdminKey));
                    }
                }
            });
        }

        static OpenAiConnection Connection() { return new OpenAiConnection { ApiKey=ApiKey,AdminKey=AdminKey,ApiKeyId=KeyId }; }
        static OpenAiSnapshot Parse(Dictionary<string,object> page) { return OpenAiSnapshot.Parse(new[]{page},KeyId,At); }
        static Dictionary<string,object> Result(long input,long output,long cached,long requests) {
            return new Dictionary<string,object> {
                {"object","organization.usage.completions.result"},{"api_key_id",KeyId},
                {"input_tokens",input},{"output_tokens",output},{"input_cached_tokens",cached},{"num_model_requests",requests}
            };
        }
        static Dictionary<string,object> Bucket(DateTimeOffset start,params Dictionary<string,object>[] results) {
            return new Dictionary<string,object> {
                {"object","bucket"},{"start_time",start.ToUnixTimeSeconds()},{"end_time",start.AddHours(1).ToUnixTimeSeconds()},{"results",results}
            };
        }
        static Dictionary<string,object> Page(bool more,string next,params Dictionary<string,object>[] buckets) {
            return new Dictionary<string,object> {{"object","page"},{"data",buckets},{"has_more",more},{"next_page",next}};
        }
        static HttpResponseMessage Response(HttpStatusCode code,string json) {
            return new HttpResponseMessage(code) { Content=new StringContent(json,System.Text.Encoding.UTF8,"application/json") };
        }
        static Dictionary<string,string> Query(string query) {
            var values=new Dictionary<string,string>();
            foreach(var item in query.TrimStart('?').Split('&')) {
                var parts=item.Split(new[]{'='},2);
                values.Add(Uri.UnescapeDataString(parts[0]),parts.Length==2?Uri.UnescapeDataString(parts[1]):"");
            }
            return values;
        }
        static void Assert(bool condition) { if(!condition) throw new Exception("Assertion failed"); }
        static void Reject(Action action) {
            bool rejected=false;try { action(); } catch(InvalidDataException) { rejected=true; }Assert(rejected);
        }
        static ProviderException ProviderFailure(Action action) {
            try { action(); } catch(ProviderException error) { return error; }throw new Exception("Expected ProviderException");
        }
        sealed class RequestInfo {
            public string Uri,Method,Authorization;
            public bool HasContent;
        }
        sealed class MockHandler : HttpMessageHandler {
            readonly Func<HttpRequestMessage,int,HttpResponseMessage> respond;
            public readonly List<RequestInfo> Requests=new List<RequestInfo>();
            public MockHandler(Func<HttpRequestMessage,int,HttpResponseMessage> respond) { this.respond=respond; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) {
                int index=Requests.Count;
                Requests.Add(new RequestInfo {
                    Uri=request.RequestUri.AbsoluteUri,Method=request.Method.Method,
                    Authorization=request.Headers.Authorization==null?null:request.Headers.Authorization.ToString(),HasContent=request.Content!=null
                });
                try { return Task.FromResult(respond(request,index)); }
                catch(Exception error) { var result=new TaskCompletionSource<HttpResponseMessage>();result.SetException(error);return result.Task; }
            }
        }
    }
}
