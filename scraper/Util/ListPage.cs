using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace scraper.Util
{
    public class BasePage
    {
        public Uri Uri { get; protected set; }

        protected static String CreateMd5(String input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                Byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                Byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (Byte b in hashBytes)
                {
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public virtual String Filename
        {
            get
            {
                String root = CreateMd5(Uri.AbsoluteUri);
                if (String.IsNullOrWhiteSpace(root) || root == "/")
                {
                    return null;
                }

                return $"{root}";
            }
        }
    }

    public class ApiPage : BasePage
    {
        public virtual String Filename => $"{base.Filename}.json";

        public ApiPage(String url)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(new Uri(url).Query);
            UriBuilder ub = new UriBuilder(url) { Query = nvc.ToString() };
            Uri = ub.Uri;
        }
    }

    public class ListPage : BasePage
    {
        private const String PageKey = "currentPage";
        private const String SizeKey = "pageSize";

        public Int32 Page { get; set; }

        public Int32 Size { get; set; }

        public virtual String Filename => $"{base.Filename}.{Page}-{Size}.html";

        public ListPage(String url, Int32? page = null, Int32? size = null)
        {
            NameValueCollection nvc = HttpUtility.ParseQueryString(new Uri(url).Query);
            if (nvc.HasKeys())
            {
                Int32.TryParse(nvc[PageKey], out Int32 parsedpage);
                Int32.TryParse(nvc[SizeKey], out Int32 parsedsize);
                page = page ?? parsedpage;
                size = size ?? parsedsize;
            }

            Page = page ?? 1;
            Size = size ?? 24;

            nvc[PageKey] = Page.ToString();
            nvc[SizeKey] = Size.ToString();

            UriBuilder ub = new UriBuilder(url) { Query = nvc.ToString() };
            Uri = ub.Uri;
        }
    }
}
