using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace scraper.Util
{
    public class CookieAwareWebClient : WebClient
    {
        private CookieContainer _container;

        public CookieAwareWebClient(CookieContainer container)
        {
            _container = container;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;

            if (webRequest != null)
            {
                webRequest.CookieContainer = _container;

                webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                webRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "");
                webRequest.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9,es;q=0.8");
                webRequest.Headers.Add(HttpRequestHeader.CacheControl, "max-age=0");
                webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
                webRequest.Headers.Add("dnt", "1");
                //webRequest.Headers.Add(HttpRequestHeader.Cookie, "prov=17349b86-5146-c013-9049-5f223a17d7b5; hero=mini; notice-CoC=4%3B1534270649749; hero-dismissed=1534270650703!lso-mini_a");
            }

            return request;
        }
    }
}
