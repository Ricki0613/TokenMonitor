using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TokenMonitor {
    internal static class UpdateTests {
        static void Assert(bool value) {if(!value)throw new Exception("Update assertion failed");}
        internal static string Release(string tag) {return Json.Write(new {tag_name=tag,draft=false,prerelease=false,html_url="https://example.com/untrusted"});}
        public static void Run(Action<string,Action> check) {
            check("Updates compare versions numerically across major, minor and patch",()=>{
                var current=new Version(2,1,0,0);
                foreach(var tag in new[]{"v2.1.1","v2.10.0","v3.0.0","2.2.0"})Assert(UpdateClient.Parse(Release(tag)).Version>current);
                foreach(var tag in new[]{"v2.1.0","v2.0.9","v1.99.99"})Assert(UpdateClient.Parse(Release(tag)).Version<=current);
            });
            check("Updates ignore draft and prerelease releases",()=>{
                Assert(UpdateClient.Parse("{\"draft\":true,\"prerelease\":false}")==null);
                Assert(UpdateClient.Parse("{\"draft\":false,\"prerelease\":true}")==null);
            });
            check("Updates reject invalid versions and unexpected response shapes",()=>{
                foreach(var body in new[]{"{}","null",Release("v2.2.0-beta.1"),Release("v2.2"),Release("v2.2.0.0"),Release("v02.2.0"),Release("v2.2.0\n"),Release("../../evil"),Release("v99999999999.0.0")}) {
                    bool rejected=false;try{UpdateClient.Parse(body);}catch(InvalidDataException){rejected=true;}Assert(rejected);
                }
            });
            check("Update links stay in the project regardless of remote html_url",()=>{
                Assert(UpdateClient.Parse(Release("v2.2.0")).Url=="https://github.com/Ricki0613/TokenMonitor/releases/tag/v2.2.0");
            });
            check("Update request uses public endpoint without account authorization",()=>{
                var handler=new Stub((request)=>{
                    Assert(request.Method==HttpMethod.Get&&request.RequestUri.AbsoluteUri==UpdateClient.Endpoint);
                    Assert(request.Headers.Authorization==null&&request.Headers.UserAgent.ToString().Contains("TokenMonitor/2.1.0"));
                    return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Release("v2.2.0"))};
                });
                using(var client=new UpdateClient(handler))Assert(client.Fetch().GetAwaiter().GetResult().Tag=="v2.2.0");
            });
            check("Update HTTP failures never report that the installed version is latest",()=>{
                foreach(var status in new[]{HttpStatusCode.Forbidden,(HttpStatusCode)429,HttpStatusCode.NotFound,HttpStatusCode.InternalServerError,HttpStatusCode.Redirect}) {
                    using(var client=new UpdateClient(new Stub(r=>new HttpResponseMessage(status)))) {
                        bool failed=false;try{client.Fetch().GetAwaiter().GetResult();}catch(InvalidOperationException){failed=true;}catch(HttpRequestException){failed=true;}Assert(failed);
                    }
                }
            });
            check("Update network failure and cancellation propagate for retry",()=>{
                foreach(bool cancel in new[]{false,true})using(var client=new UpdateClient(new Stub(r=>{if(cancel)throw new TaskCanceledException();throw new HttpRequestException();}))) {
                    bool failed=false;try{client.Fetch().GetAwaiter().GetResult();}catch(TaskCanceledException){failed=true;}catch(HttpRequestException){failed=true;}Assert(failed);
                }
            });
        }
        sealed class Stub : HttpMessageHandler {
            readonly Func<HttpRequestMessage,HttpResponseMessage> send;
            public Stub(Func<HttpRequestMessage,HttpResponseMessage> send) {this.send=send;}
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken) {return Task.FromResult(send(request));}
        }
    }
}
