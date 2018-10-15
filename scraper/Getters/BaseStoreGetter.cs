using CsvHelper;
using CsvHelper.Configuration;
using scraper.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace scraper
{
    public abstract class BaseStoreGetter : IStoreGetter
    {
        public abstract String StoreName { get; }

        internal PageGetter _getter { get; set; }
        internal PageGetter _upcGetter { get; set; }

        internal readonly Queue<ListPage> listPages = new Queue<ListPage>();
        internal readonly List<String> productPages = new List<String>();
        internal readonly List<Product> productData = new List<Product>();

        public virtual void InitPageGetter()
        {
            _upcGetter = new PageGetter
            {
                Store = "upc",
            };
            _getter = new PageGetter
            {
                Store = StoreName,
            };
        }

        public abstract void Get();

        internal BaseStoreGetter()
        {
            InitPageGetter();
        }

        internal void Output(IEnumerable<Product> products)
        {
            String folder = Path.Combine(Scraper.Path, _getter.Store);

            using (StreamWriter txt = File.CreateText(Path.Combine(folder, $"products.{DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.csv")))
            using (CsvWriter csv = new CsvWriter(txt, new Configuration
            {
                Encoding = Encoding.Unicode,
                TrimOptions = TrimOptions.InsideQuotes,
            }))
            {
                csv.WriteRecords(products);
            }
        }
    }
}
