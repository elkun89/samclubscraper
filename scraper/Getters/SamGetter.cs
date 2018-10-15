using CsvHelper;
using HtmlAgilityPack;
using scraper.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Nager.ArticleNumber;
using Newtonsoft.Json.Linq;

namespace scraper
{
    public class SamsGetter : BaseStoreGetter
    {
        public override String StoreName { get; } = "samsclub";

        public override void InitPageGetter()
        {
            base.InitPageGetter();

            _getter.CookieJar.Add(new System.Net.Cookie("myPreferredClub", "6217", "/", ".samsclub.com")
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false,
                Secure = true,
            });

            _getter.CookieJar.Add(new System.Net.Cookie("myPreferredClubName", "Miami%2C+FL", "/", ".samsclub.com")
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false,
                Secure = true,
            });

            _getter.CookieJar.Add(new System.Net.Cookie("plpprefs", "viewMode=grid", "/", ".samsclub.com")
            {
                Expires = DateTime.MinValue,
                HttpOnly = false,
                Secure = false,
            });

            _getter.CookieJar.Add(new System.Net.Cookie("AB_param", "polaris", "/", ".samsclub.com")
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false,
                Secure = false,
            });

            _getter.CookieJar.Add(new System.Net.Cookie("testVersion", "versionA", "/", ".samsclub.com")
            {
                Expires = DateTime.MinValue,
                HttpOnly = false,
                Secure = false,
            });
        }

        public override void Get()
        {
            const String root = "https://www.samsclub.com/sams";
            listPages.Enqueue(new ListPage(root + "/grocery/1444.cp?xid=hdr_shop2_grocery_shop-all&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/office-supplies/1706.cp?xid=hdr_shop2_office_shop-all&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/health-and-beauty/1585.cp?xid=hdr_shop2_pharmacy-health-beauty_shop-all&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/baby-supplies/1946.cp?xid=hdr_shop2_baby-toddler_shop-all&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/sports-equipment-fitness-equipment/1888.cp?xid=cat100001-all%20:subcat:2:34&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/home-collection/1285.cp?xid=hdr_shop2_home-and-appliances_shop-all&saclp=1"));
            listPages.Enqueue(new ListPage(root + "/sams/household-essentials/450203.cp?xid=hdr_shop2_household-essentials-pets_shop-all&saclp=1"));

            Action<String> traverseLeftNav = _ => { };
            Action<HtmlDocument> scrapeProducts = _ => { };
            HashSet<String> processedLists = new HashSet<String>();
            HashSet<String> processedProductPages = new HashSet<String>();

            traverseLeftNav = (url) =>
            {
                if (!processedLists.Add(url))
                {
                    return;
                }

                scrapeProducts = (productPageDoc) =>
                {
                    List<String> newProducts = productPageDoc.DocumentNode
                        .Descendants()
                        .Where(node => node.HasClass("sc-product-card"))
                        .SelectMany(pager => pager
                            .Descendants()
                            .Where(node => node.HasClass("sc-product-card-title")))?
                        .Select(node => node?.Element("a"))?
                        .Select(node =>
                        {
                            Console.WriteLine("  p:" + node.InnerText);
                            return node?.GetAttributeValue("href", "nope");
                        })
                        .Where(href => !String.IsNullOrWhiteSpace(href) && href != "#")
                        .Select(href => new Uri(new Uri(url), href).AbsoluteUri)
                        .ToList();

                    productPages.AddRange(newProducts);

                    Console.WriteLine($"Added {newProducts.Count} products");
                };

                HtmlDocument doc = _getter.GetPage(url);

                HtmlNode[] leftNav = doc.DocumentNode.Descendants()
                    .Where(node => node.HasClass("catLeftNav"))
                    .ToArray();

                if (leftNav.Any())
                {
                    List<String> subcategories = leftNav
                        .SelectMany(list => list.Descendants().Where(l => l.Attributes.Contains("href")))
                        .Select(node =>
                        {
                            Console.WriteLine("cat:" + node.InnerText);
                            return node?.GetAttributeValue("href", "nope");
                        })
                        .Where(href => !String.IsNullOrWhiteSpace(href) && href != "#")
                        .Select(href => new Uri(new Uri(url), href).AbsoluteUri + "&limit=1000&offset=0")
                        .ToList();

                    Console.WriteLine($"Added {subcategories.Count} categories");
                    subcategories.ForEach(traverseLeftNav);
                }
                else
                {
                    scrapeProducts(doc);
                }
            };

            String sanitize(String text) => HttpUtility.HtmlDecode(text ?? String.Empty)
                .Trim()
                .Replace('‘', '\'')
                .Replace('’', '\'')
                .Replace("“", "\\\"")
                .Replace("”", "\\\"")
                .Replace("â€\"", "—")
                .Replace("fÄ\"", "–")
            ;

            Regex imageIdPattern = new Regex("imageList = '(?<upc>[0-9]+)'", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            Regex imageNamer = new Regex("image/samsclub/[0-9]+_(?<Key>[A-E])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            Regex upcPattern = new Regex("image/samsclub/(?<upc>[0-9]+)_", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            Regex modelPattern = new Regex("<tr><td>Model #:</td><td>(?<model>[A-Z0-9]+)</td></tr>?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
            Regex slugPattern = new Regex("[^A-Z0-9 ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            while (listPages.Count > 0)
            {
                ListPage listPage = listPages.Dequeue();

#if SINGLE_TRIAL
                productPages.Clear();
                productPages.Add("https://www.samsclub.com/sams/dixie-perfectouch-hot-cups-12-oz-160-ct/201086.ip?xid=plp_product_1_20");
#else
                traverseLeftNav(listPage.Uri.AbsoluteUri);
#endif

                IEnumerable<String> currentProductPages = productPages.Except(processedProductPages).ToArray();

                foreach (String productUrl in currentProductPages)
                {
                    if (!processedProductPages.Add(productUrl))
                    {
                        continue;
                    }

                    HtmlDocument productPage = _getter.GetPage(productUrl);
                    IEnumerable<HtmlNode> nodes = productPage.DocumentNode.Descendants().ToArray();

                    ILookup<String, HtmlNode> itemprops = nodes
                        .Where(n => n.HasAttributes)
                        .Where(n => n.GetAttributeValue("itemprop", String.Empty) != String.Empty)
                        .ToLookup(n => n.GetAttributeValue("itemprop", String.Empty));

                    String getItemPropValue(String prop) => itemprops[prop].FirstOrDefault()?.InnerText;
                    String getValueByID(String id) => productPage.GetElementbyId(id)?.GetAttributeValue("value", String.Empty);

                    String[] categories = itemprops["title"]
                                                .Skip(1)
                                                .Select(node => sanitize(node.InnerText))
                                                .ToArray();
                    String categoryPath = String.Join(" > ", categories);
                    String priceJSON = productPage.GetElementbyId("moneyBoxJson")?.InnerText;

                    String partialUPC = imageIdPattern.Match(productPage.ParsedText).Groups["upc"].Value;
                    Dictionary<String, String> images = new Dictionary<String, String>();
                    if (!String.IsNullOrWhiteSpace(partialUPC))
                    {
                        JObject imageJson = _getter.GetJson($"https://www.samsclub.com/api/product/{partialUPC}/images");
                        images = imageJson["Images"]
                            .Select(t => t["ImageUrl"].ToString())
                            .ToLookup(src => imageNamer.Match(src).Groups["Key"].Value)
                            .ToDictionary(key => key.Key, grp => grp.First());
                    }

                    Product product = null;

                    if (priceJSON != null)
                    {
                        priceJSON = sanitize(priceJSON);
                        dynamic productPriceData = Newtonsoft.Json.JsonConvert.DeserializeObject(priceJSON);

                        if (productPriceData == null || productPriceData.availableSKUs.Count <= 0)
                        {
                            continue;
                        }

                        List<Tuple<String, String, String>> skuVariances = new List<Tuple<String, String, String>>();

                        if (productPriceData.availableVariances != null)
                        {
                            foreach (dynamic variance in productPriceData.availableVariances)
                            {
                                foreach (dynamic varianceValue in variance.Value.varianceValuesMap)
                                {
                                    JArray applicableSkusArray = (JArray)varianceValue.Value.applicableSkus;
                                    HashSet<String> applicableSkus =
                                        applicableSkusArray.Select(jval => jval.Value<String>()).ToHashSet();

                                    foreach (String applicableSku in applicableSkus)
                                    {
                                        skuVariances.Add(Tuple.Create(applicableSku,
                                            variance.Value.varianceName.ToString(),
                                            varianceValue.Value.varianceValue.ToString()));
                                    }
                                }
                            }
                        }

                        ILookup<String, Tuple<String, String, String>> variancesBySku = skuVariances
                                .Distinct()
                                .ToLookup(tuple => tuple.Item1);

                        String description = String.Concat(productPriceData.shortDescription, Environment.NewLine, productPriceData.longDescription);

                        foreach (dynamic sku in productPriceData.availableSKUs)
                        {
                            if (sku?.onlinePriceVO?.listPrice == null)
                            {
                                continue;
                            }

                            product = new Product
                            {
                                ProductUrl = productUrl,
                                UPC = sku.upc,
                                Brand = sanitize(productPriceData.brandName),
                                Name = sanitize(productPriceData.productName),
                                VariantName = sanitize(sku.skuName),
                                Description = sanitize(description),
                                ProductID = sku.productId,
                                ItemNumber = sku.itemNo,
                                SkuID = sku.skuId,
                                ModelNumber = sku.modelNo,
                                //ImageUrl = $"//images.samsclubresources.com/is/image/samsclub/{variant.imageName}?$img_size_380x380$",
                                Price = sku.onlinePriceVO.listPrice,
                                Categories = categoryPath,
                            };

                            Int32 varCount = 1;
                            foreach ((String skuID, String varianceName, String varianceValue) in variancesBySku[product.SkuID])
                            {
                                switch (varCount)
                                {
                                    case 1:
                                        product.Variance_1 = varianceName;
                                        product.VarianceValue_1 = varianceValue;
                                        break;
                                    case 2:
                                        product.Variance_2 = varianceName;
                                        product.VarianceValue_2 = varianceValue;
                                        break;
                                    default:
                                        Debug.Fail("why are there more than two?");
                                        break;
                                }

                                varCount++;
                            }
                        }
                    }
                    else
                    {
                        String brand = itemprops["brand"]
                            .SelectMany(node => node.Descendants()
                                .Where(child => child.GetAttributeValue("itemprop", String.Empty) == "name"))
                            .FirstOrDefault()?
                            .InnerText;

                        String productName = getItemPropValue("name");
                        String description = nodes.Where(node => node.HasClass("itemDescription")).FirstOrDefault()?.InnerHtml;
                        String productID = getValueByID("mbxProductId");
                        String skuID = getValueByID("mbxSkuId");
                        String itemNumber = getValueByID("itemNo");
                        String modelNumber = getItemPropValue("model");
                        String productPrice = getItemPropValue("price");

                        if (String.IsNullOrWhiteSpace(productPrice))
                        {
                            continue;
                        }

                        product = new Product
                        {
                            ProductUrl = productUrl,
                            Name = sanitize(productName),
                            Brand = sanitize(brand),
                            Description = sanitize(description),
                            ProductID = productID,
                            ItemNumber = itemNumber,
                            ModelNumber = modelNumber,
                            SkuID = skuID,
                            Price = productPrice,
                            Categories = categoryPath,
                        };
                    }

                    if (product != null)
                    {
                        productData.Add(product);

                        if (images.ContainsKey("A")) { product.ImageUrl_A = images["A"]; }
                        if (images.ContainsKey("B")) { product.ImageUrl_B = images["B"]; }
                        if (images.ContainsKey("C")) { product.ImageUrl_C = images["C"]; }
                        if (images.ContainsKey("D")) { product.ImageUrl_D = images["D"]; }
                        if (images.ContainsKey("E")) { product.ImageUrl_E = images["E"]; }
                    }

                    if (product == null
                        || !String.IsNullOrWhiteSpace(product.UPC)
                        || String.IsNullOrWhiteSpace(product.ImageUrl_A))
                    {
                        continue;
                    }

                    partialUPC = partialUPC ?? upcPattern.Match(product.ImageUrl_A).Groups["upc"].Value;

                    for (Int32 i = 0; i < 9; i++)
                    {
                        String maybeUPC = partialUPC + i;
                        ArticleNumberType articleNumberType = ArticleNumberHelper.GetArticleNumberType(maybeUPC);

                        if (articleNumberType == ArticleNumberType.UNKNOWN) { continue; }

                        HtmlDocument upcPage = _upcGetter.GetPage("https://www.upcitemdb.com/upc/" + maybeUPC);

                        HtmlNode[] productNames = upcPage.DocumentNode.Descendants()
                            .Where(node => node.HasClass("cont"))
                            .SelectMany(node => node.Descendants())
                            .ToArray();

                        String productDetails = upcPage.DocumentNode.Descendants()
                                                    .FirstOrDefault(node => node.HasClass("detail-list"))?
                                                    .InnerHtml
                                                ?? "none";

                        String pageModel = modelPattern.Match(productDetails).Groups["model"].Value;

                        if (pageModel == partialUPC || pageModel == product.ModelNumber)
                        {
                            product.UPC = maybeUPC;
                            continue;
                        }

                        if (productNames.Any())
                        {
                            product.UPC = maybeUPC;

                            HashSet<String> nameCandidates = new HashSet<String>(new[]
                            {
                                    product.Name,
                                    product.VariantName,
                                    product.ModelNumber,
                                });

                            if (!String.IsNullOrWhiteSpace(product.Name))
                            {
                                nameCandidates.Add(slugPattern.Replace(product.Name, String.Empty));
                            }

                            if (!String.IsNullOrWhiteSpace(product.VariantName))
                            {
                                nameCandidates.Add(slugPattern.Replace(product.VariantName, String.Empty));
                            }

                            if (!String.IsNullOrWhiteSpace(product.ModelNumber))
                            {
                                nameCandidates.Add(slugPattern.Replace(product.ModelNumber, String.Empty));
                            }

                            nameCandidates.Remove(String.Empty);

                            Boolean exact = productNames.Any(n => nameCandidates.Contains(n.InnerText));

                            if (!exact)
                            {
                                product.UPC += '*';
                            }
                        }

                        break;
                    }
                }
            }

            Output(productData);
        }
    }
}
