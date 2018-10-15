using CsvHelper;
using HtmlAgilityPack;
using scraper.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scraper
{
    public class StaplesGetter
    {
        public void GetStaples()
        {
            PageGetter getter = new PageGetter()
            {
                Store = "staples"
            };

            Queue<ListPage> listPages = new Queue<ListPage>(new[] {
                new ListPage("https://www.staples.com/asgard-node/v1/nad/staplesus/deals/html/BI1415591?rank=1&supercategory=&onlybopis=false&pagenum=1&zipcode=90745&locale=en-US&productTile=secondaryDealsData"),
            });

            Queue<String> productPages = new Queue<String>();
            List<Product> productData = new List<Product>();

            Action<String> traverseLeftNav = s => { };
            Action<HtmlDocument> scrapeProducts = d => { };

            traverseLeftNav = (url) =>
            {
                scrapeProducts = (productPageDoc) =>
                {
                    List<String> newProducts = productPageDoc.DocumentNode
                        .Descendants().Where(node => node.HasClass("sc-product-card"))?
                        .SelectMany(pager => pager
                            .Descendants().Where(node => node.HasClass("sc-product-card-title")))?
                        .Select(node => node?.Element("a"))?
                        .Select(node =>
                        {
                            Console.WriteLine("  p:" + node.InnerText);
                            return node?.GetAttributeValue("href", "nope");
                        })
                        .Where(href => !String.IsNullOrWhiteSpace(href) && href != "#")
                        .Select(href => new Uri(new Uri(url), href).AbsoluteUri)
                        .ToList();

                    newProducts.ForEach(productPages.Enqueue);

                    Console.WriteLine($"Added {newProducts.Count} products");
                };

                HtmlDocument doc = getter.GetPage(url);

                IEnumerable<HtmlNode> leftNav = doc.DocumentNode.Descendants().Where(node => node.HasClass("catLeftNav"));

                if (leftNav.Any())
                {
                    Queue<String> subcategories = new Queue<String>();

                    leftNav
                        .SelectMany(list => list.Descendants().Where(l => l.Attributes.Contains("href")))
                        .Select(node =>
                        {
                            Console.WriteLine("cat:" + node.InnerText);
                            return node?.GetAttributeValue("href", "nope");
                        })
                        .Where(href => !String.IsNullOrWhiteSpace(href) && href != "#")
                        .Select(href => new Uri(new Uri(url), href).AbsoluteUri + "&limit=1000&offset=0")
                        .ToList()
                        .ForEach(subcategories.Enqueue);

                    Console.WriteLine($"Added {subcategories.Count} categories");
                    while (subcategories.Count > 0)
                    {
                        traverseLeftNav(subcategories.Dequeue());
                    }
                }
                else
                {
                    scrapeProducts(doc);
                }
            };

            while (listPages.Count > 0)
            {
                ListPage listPage = listPages.Dequeue();

                traverseLeftNav(listPage.Uri.AbsoluteUri);

                productPages = new Queue<String>(productPages.Distinct());

                while (productPages.Count > 0)
                {
                    String productUrl = productPages.Dequeue();
                    HtmlDocument productPage = getter.GetPage(productUrl);
                    IEnumerable<HtmlNode> nodes = productPage.DocumentNode.Descendants();

                    Func<String, Func<HtmlNode, Boolean>> getByItemProp = prop => {
                        return node => node.GetAttributeValue("itemprop", String.Empty) == prop;
                    };

                    String productName = nodes.Where(getByItemProp("name")).FirstOrDefault()?.InnerText;
                    String description = nodes.Where(node => node.HasClass("itemDescription")).FirstOrDefault()?.InnerHtml;
                    String productID = nodes.Where(getByItemProp("productID")).FirstOrDefault()?.InnerText;
                    String image = nodes.Where(getByItemProp("image")).FirstOrDefault()?.GetAttributeValue("src", String.Empty);
                    String productPrice = nodes.Where(getByItemProp("price")).FirstOrDefault()?.InnerText;

                    if (!String.IsNullOrWhiteSpace(productPrice))
                    {
                        String[] categories = nodes.Where(getByItemProp("title"))
                            .Skip(1)
                            .Select(node => node.InnerText)
                            .ToArray();

                        productData.Add(new Product
                        {
                            Name = productName,
                            Description = description,
                            ProductID = productID,
                            //ImageUrl = image,
                            Price = productPrice,
                            Categories = String.Join(" > ", categories),
                        });
                    }
                }
            }

            String folder = Path.Combine(Scraper.Path, getter.Store);

            using (StreamWriter txt = File.CreateText(Path.Combine(folder, $"products.{DateTime.Now.ToString("yyyy-MM-dd-HHmm")}.csv")))
            using (CsvWriter csv = new CsvWriter(txt))
            {
                csv.WriteRecords(productData);
            }
        }
    }
}
