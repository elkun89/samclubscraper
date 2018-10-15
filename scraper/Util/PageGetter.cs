using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace scraper.Util
{
    public class PageGetter
    {
        public const Int32 PageLoadDelay = 2;
        public Boolean Enabled { get; private set; } = true;

        private Int32 _loaded = 0;

        private String Folder => Path.Combine(Scraper.Path, Store);
        private readonly CookieAwareWebClient _webclient;

        private String _store;
        public String Store
        {
            get => _store;
            set
            {
                _store = value;
                Directory.CreateDirectory(Folder);
            }
        }

        public CookieContainer CookieJar { get; } = new CookieContainer();

        public PageGetter()
        {
            _webclient = new CookieAwareWebClient(CookieJar);
        }

        private JObject GetJson(String url, String filePath)
        {
            JObject json = new JObject();

            if (!Enabled)
            {
                return json;
            }

            if (File.Exists(filePath))
            {
                String jsonText = File.ReadAllText(filePath);
                json = JsonConvert.DeserializeObject(jsonText) as JObject;
            }
            else
            {
                try
                {
                    _webclient.DownloadFile(url, filePath);
                    _loaded++;
                    String jsonText = File.ReadAllText(filePath);
                    json = JsonConvert.DeserializeObject(jsonText) as JObject;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed {url}");
                    Console.WriteLine(ex.Message);
                }
            }

            return json;
        }

        private HtmlDocument GetPage(String url, String filePath)
        {
            HtmlDocument doc = new HtmlDocument();

            if (!Enabled)
            {
                return doc;
            }

            //if (!File.Exists(filePath))
            //{
            //    string otherFileName = new ListPage(HttpUtility.HtmlEncode(url)).Filename;
            //    string otherFilePath = Path.Combine(_folder, otherFileName);

            //    if (File.Exists(otherFilePath))
            //    {
            //        File.Move(otherFilePath, filePath);
            //    }
            //}

            if (File.Exists(filePath))
            {
                doc.Load(filePath, Encoding.UTF8);
                Console.WriteLine($"Loaded {filePath}");
            }
            else
            {
                try
                {
                    url = url
                            .Replace("&#039;", "'")
                        ;

                    url = HttpUtility.HtmlDecode(url);
                    _webclient.DownloadFile(url, filePath);
                    _loaded++;
                    doc.Load(filePath);
                    Console.WriteLine($"Saved {filePath}");
                }
                catch (WebException wex)
                {
                    HttpWebResponse webResponse = wex.Response as HttpWebResponse;
                    Int32 status = (Int32?)webResponse?.StatusCode ?? (Int32)wex.Status;

                    if (status == 429)
                    {
                        Enabled = false;
                    }
                    else
                    {
                        File.WriteAllText(filePath, webResponse?.StatusCode.ToString() ?? wex.Status.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed {url}");
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(TimeSpan.FromSeconds(PageLoadDelay));
            }

            return doc;
        }

        public JObject GetJson(String url)
        {
            String fileName = new ApiPage(url).Filename;
            String filePath = Path.Combine(Folder, fileName);

            return GetJson(url, filePath);
        }

        public HtmlDocument GetPage(String url)
        {
            String fileName = new ListPage(url).Filename;
            String filePath = Path.Combine(Folder, fileName);

            return GetPage(url, filePath);
        }

        public HtmlDocument GetPage(ListPage page)
        {
            String fileName = page.Filename ?? (Store + ".html");
            String filePath = Path.Combine(Folder, fileName);

            return GetPage(page.Uri.AbsoluteUri, filePath);
        }
    }
}
