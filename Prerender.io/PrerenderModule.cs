using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Prerender.io
{
    public class PrerenderModule : IHttpModule
    {
        private PrerenderConfigSection _prerenderConfig;
        private HttpApplication _context;
        private static readonly string PRERENDER_SECTION_KEY = "prerender";
        private static readonly string _Escaped_Fragment = "_escaped_fragment_";
        private List<string> CrawlerUsageAgents { get; set; }
        private List<string> ExtensionsToIgnoreStrings { get; set; }
        private List<Regex> WhiteList { get; set; }
        private List<Regex> BlackList { get; set; }


        public void Dispose()
        {

        }

        public void Init(HttpApplication context)
        {
            this._context = context;
            _prerenderConfig = ConfigurationManager.GetSection(PRERENDER_SECTION_KEY) as PrerenderConfigSection;
            BuildCrawlerUsageAgentsList();
            BuildExtensionsToIgnoreList();
            BuildBlackList();
            BuildWhiteList();
            var wrapper = new EventHandlerTaskAsyncHelper(ContextBeginRequest);
            context.AddOnBeginRequestAsync(wrapper.BeginEventHandler, wrapper.EndEventHandler);
        }

        protected async Task ContextBeginRequest(object sender, EventArgs e)
        {
            try
            {
                await DoPrerenderAsync(_context);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());
            }
        }

        private async Task DoPrerenderAsync(HttpApplication context)
        {
            var httpContext = context.Context;
            var request = httpContext.Request;
            var response = httpContext.Response;
            if (ShouldShowPrerenderedPage(request))
            {
                var result = await GetPrerenderedPageResponseAsync(request);

                response.StatusCode = (int)result.StatusCode;

                // The WebHeaderCollection is horrible, so we enumerate like this!
                // We are adding the received headers from the prerender service
                for (var i = 0; i < result.Headers.Count; ++i)
                {
                    var header = result.Headers.GetKey(i);
                    var values = result.Headers.GetValues(i);

                    if (values == null) continue;

                    foreach (var value in values)
                    {
                        response.Headers.Add(header, value);
                    }
                }

                response.Write(result.ResponseBody);
                response.Flush();
                context.CompleteRequest();
            }
        }


        private async Task<ResponseResult> GetPrerenderedPageResponseAsync(HttpRequest request)
        {
            var apiUrl = GetApiUrl(request);
            var webRequest = (HttpWebRequest)WebRequest.Create(apiUrl);
            webRequest.Method = "GET";
            webRequest.UserAgent = request.UserAgent;
            webRequest.AllowAutoRedirect = false;
            SetProxy(webRequest);
            SetNoCache(webRequest);

            // Add our key!
            if (!string.IsNullOrWhiteSpace(_prerenderConfig.Token))
            {
                webRequest.Headers.Add("X-Prerender-Token", _prerenderConfig.Token);
            }


            try
            {
                // Get the web response and read content etc. if successful
                using (var webResponse = (HttpWebResponse)await webRequest.GetResponseAsync())
                {
                    using (var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        return new ResponseResult(webResponse.StatusCode, await reader.ReadToEndAsync(), webResponse.Headers);
                    }
                }
            }
            catch (WebException e)
            {
                // Handle response WebExceptions for invalid renders (404s, 504s etc.) - but we still want the content
                var reader = new StreamReader(e.Response.GetResponseStream(), Encoding.UTF8);
                return new ResponseResult(((HttpWebResponse)e.Response).StatusCode, await reader.ReadToEndAsync(), e.Response.Headers);
            }
        }

        private void SetProxy(HttpWebRequest webRequest)
        {
            if (_prerenderConfig.Proxy != null && !string.IsNullOrWhiteSpace(_prerenderConfig.Proxy.Url))
            {
                webRequest.Proxy = new WebProxy(_prerenderConfig.Proxy.Url, _prerenderConfig.Proxy.Port);
            }
        }

        private static void SetNoCache(HttpWebRequest webRequest)
        {
            webRequest.Headers.Add("Cache-Control", "no-cache");
            webRequest.ContentType = "text/html";
        }

        private String GetApiUrl(HttpRequest request)
        {
            // var url = request.Url.AbsoluteUri; (not working with angularjs)
            // use request.RawUrl instead of request.Url.AbsoluteUri to get the original url
            // becuase angularjs requires a rewrite and requests are rewritten to base /
            var url = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, request.RawUrl);

            // request.RawUrl have the _escaped_fragment_ query string
            // Prerender server remove it before making a request, but caching plugins happen before prerender server remove it
            url = RemoveQueryStringByKey(url, "_escaped_fragment_");

            // Correct for HTTPS if that is what the request arrived at the load balancer as 
            // (AWS and some other load balancers hide the HTTPS from us as we terminate SSL at the load balancer!)
            if (string.Equals(request.Headers["X-Forwarded-Proto"], "https", StringComparison.InvariantCultureIgnoreCase))
            {
                url = url.Replace("http://", "https://");
            }

            // Remove the application from the URL
            if (_prerenderConfig.StripApplicationNameFromRequestUrl && !string.IsNullOrEmpty(request.ApplicationPath) && request.ApplicationPath != "/")
            {
                // http://test.com/MyApp/?_escape_=/somewhere
                url = url.Replace(request.ApplicationPath, string.Empty);
            }

            var prerenderServiceUrl = _prerenderConfig.PrerenderServiceUrl;
            return prerenderServiceUrl.EndsWith("/")
                ? (prerenderServiceUrl + url)
                : string.Format("{0}/{1}", prerenderServiceUrl, url);
        }

        public static string RemoveQueryStringByKey(string url, string key)
        {
            var uri = new Uri(url);

            // this gets all the query string key value pairs as a collection
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);

            // this removes the key if exists
            newQueryString.Remove(key);

            // this gets the page path from root without QueryString
            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return newQueryString.Count > 0
                ? String.Format("{0}?{1}", pagePathWithoutQueryString, newQueryString)
                : pagePathWithoutQueryString;
        }


        private bool ShouldShowPrerenderedPage(HttpRequest request)
        {
            var userAgent = request.UserAgent;
            var url = request.Url;
            var referer = request.UrlReferrer == null ? string.Empty : request.UrlReferrer.AbsoluteUri;

            if (IsInBlackList(url, referer))
            {
                return false;
            }

            if (IsInWhiteList(url))
            {
                return false;
            }

            if (HasEscapedFragment(request))
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return false;
            }

            if (!IsInSearchUserAgent(userAgent))
            {
                return false;
            }
            if (IsInResources(url))
            {
                return false;
            }
            return true;

        }

        private bool IsInBlackList(Uri url, string referer)
        {
            return this.BlackList.Any(n => n.IsMatch(url.AbsoluteUri) || (!string.IsNullOrWhiteSpace(referer) && n.IsMatch(referer)));
        }

        private bool IsInWhiteList(Uri url)
        {
            return this.WhiteList.Any(n => n.IsMatch(url.AbsoluteUri));
        }

        private bool IsInResources(Uri url)
        {
            var extensionsToIgnore = this.ExtensionsToIgnoreStrings;
            return extensionsToIgnore.Any(item => url.AbsoluteUri.ToLower().Contains(item.ToLower()));
        }

        private bool IsInSearchUserAgent(string useAgent)
        {
            //var crawlerUserAgents = GetCrawlerUserAgents();
            var crawlerUserAgents = this.CrawlerUsageAgents;

            // We need to see if the user agent actually contains any of the partial user agents we have!
            // THE ORIGINAL version compared for an exact match...!
            return
                (crawlerUserAgents.Any(
                    crawlerUserAgent =>
                    useAgent.IndexOf(crawlerUserAgent, StringComparison.InvariantCultureIgnoreCase) >= 0));
        }

        private bool HasEscapedFragment(HttpRequest request)
        {
            return request.QueryString.AllKeys.Contains(_Escaped_Fragment);
        }

        private void BuildBlackList()
        {
            var blackList = new List<Regex>();
            blackList.AddRange(_prerenderConfig.Blacklist.Select(n => new Regex(n)));
            this.BlackList = blackList;
        }

        private void BuildWhiteList()
        {
            var whiteList = new List<Regex>();
            whiteList.AddRange(_prerenderConfig.Whitelist.Select(n => new Regex(n)));
            this.WhiteList = whiteList;
        }

        private void BuildExtensionsToIgnoreList()
        {
            var extensionsToIgnore = new List<string>();
            extensionsToIgnore.AddRange(PredefinedValues.ExtensionsToIgnore);
            extensionsToIgnore.AddRange(_prerenderConfig.ExtensionsToIgnore);
            this.ExtensionsToIgnoreStrings = extensionsToIgnore.Select(n => n.ToLower()).Distinct().ToList();
        }

        private void BuildCrawlerUsageAgentsList()
        {
            var userAgents = new List<string>();
            userAgents.AddRange(PredefinedValues.CrawlerUserAgents);
            userAgents.AddRange(_prerenderConfig.CrawlerUserAgents);
            this.CrawlerUsageAgents = userAgents.Select(n => n.ToLower()).Distinct().ToList();
        }
    }
}
