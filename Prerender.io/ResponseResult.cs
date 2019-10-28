using System.Net;

namespace Prerender.io
{
    public class ResponseResult
    {
        public HttpStatusCode StatusCode
        {
            private set;
            get;
        }

        public string ResponseBody
        {
            private set;
            get;
        }

        public WebHeaderCollection Headers
        {
            private set;
            get;
        }

        public ResponseResult(HttpStatusCode code, string body, WebHeaderCollection headers)
        {
            StatusCode = code;
            ResponseBody = body;
            Headers = headers;
        }

    }
}
