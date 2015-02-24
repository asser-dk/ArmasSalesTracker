﻿namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Web;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using HtmlAgilityPack;
    using log4net;

    public class ArmasScraper : IArmasScraper
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConfiguration configuration;

        private readonly Regex tabTitleRegex = new Regex(@"\bnav_\w+\b");

        public ArmasScraper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Cookie SessionCookie { get; set; }

        public IEnumerable<ProductLine> GetArmasProductLines()
        {
            Log.Debug("Get all armas products");

            LogInToArmas(configuration.ArmasUsername, configuration.ArmasPassword);

            return
                GetTabs()
                    .SelectMany(tabInfo => GetSubPages(tabInfo.Url), (tabInfo, page) => new { tabInfo, page })
                    .SelectMany(@t => GetProductLines(@t.page, @t.tabInfo));
        }

        public IEnumerable<PageInfo> GetTabs()
        {
            Log.Debug("Get tabs");
            var web = new HtmlWeb();
            var doc = web.Load(configuration.ArmasFrontpagePageUri);
            var tabLinksNode = doc.DocumentNode.SelectNodes("//div[@id='product_categories_g1c']//a[@href]");

            foreach (var tabLink in tabLinksNode)
            {
                var classes = tabLink.Attributes["class"].Value;
                var matches = tabTitleRegex.Matches(classes);
                var title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(matches[0].Value.Substring(4).Replace('_', ' '));
                var url = configuration.ArmasBaseHost + tabLink.Attributes["href"].Value;

                Log.Debug(string.Format("Found tab {0} with the link {1}", title, url));
                yield return new PageInfo
                {
                    Title = title,
                    Url = url
                };
            }
        }

        public IEnumerable<PageInfo> GetSubPages(PageInfo tabInfo)
        {
            Log.Debug(string.Format("Get sub pages for page {1} ({0})", tabInfo.Url, tabInfo.Title));
            var web = new HtmlWeb();
            var doc = web.Load(tabInfo.Url);

            var subPagesNode = doc.DocumentNode.SelectNodes("//div[@id='product_subcategories_g1c']/ul/li");

            foreach (var subPageNode in subPagesNode)
            {
                var pageInfo = new PageInfo();

                var anchorNode = subPageNode.SelectSingleNode("a");

                if (anchorNode != null)
                {
                    pageInfo.Title = anchorNode.InnerText;
                    pageInfo.Url = string.Format("{0}/{1}", configuration.ArmasBaseHost, anchorNode.Attributes["href"].Value);
                }
                else
                {
                    pageInfo.Url = tabInfo.Url;
                    pageInfo.Title = subPageNode.InnerText;
                }

                Log.Debug(string.Format("Found the subpage {0} with the link {1}", pageInfo.Title, pageInfo.Url));

                yield return pageInfo;
            }
        }

        public IEnumerable<ProductLine> GetProductLines(PageInfo pageInfo, PageInfo tabInfo)
        {
            Log.Info(string.Format("Get product lines for page {1} ({0})", pageInfo.Url, pageInfo.Title));
            var web = new HtmlWeb();
            var doc = web.Load(pageInfo.Url);
            var productLinksNode = doc.DocumentNode.SelectNodes("//div[@id='products']/div/div[starts-with(@class, 'product_listing')]");

            foreach (var productLineNode in productLinksNode)
            {
                var productLine = new ProductLine();
                productLine.Category = string.Format("{0} - {1}", tabInfo.Title, pageInfo.Title);

                productLine.Id = productLineNode.Id.Substring(7);

                productLine.Url = string.Format(
                    "{0}/{1}",
                    configuration.ArmasBaseUrl,
                    productLineNode.SelectSingleNode("table/tr/td[@class='product_image_container']/a").Attributes["href"].Value);

                productLine.ImageUrl = configuration.ArmasBaseHost + productLineNode.SelectSingleNode("table/tr/td/a/img").Attributes["src"].Value;

                productLine.Title = productLineNode.SelectSingleNode("h4").InnerText;

                productLine.Price =
                    int.Parse(productLineNode.SelectSingleNode("table/tr/td[@class='product_price_container']/span/b/span[@class='product_g1c_price']").InnerText.Replace(" G1C", string.Empty));

                var premiumPriceNode =
                    productLineNode.SelectSingleNode("table/tr/td[@class='product_price_container']/span/b/span/span[contains(@class, 'premium_price')]");

                if (premiumPriceNode != null)
                {
                    productLine.PremiumPrice =
                    int.Parse(
                        premiumPriceNode
                            .InnerText.Replace(" G1C", string.Empty));
                }

                Log.Debug(
                    string.Format(
                        "Found the product {0} (id: {1}) with the link {2}",
                        productLine.Title,
                        productLine.Id,
                        productLine.Url));

                yield return productLine;
            }
        }

        private void LogInToArmas(string username, string password)
        {
            Log.Debug("Log in to armas");
            var web = new HtmlWeb { UseCookies = true, PostResponse = OnPostResponse };
            var doc = web.Load(configuration.ArmasLoginPageUrl);
            var loginToken = doc.DocumentNode.SelectSingleNode("//*[@id=\"login__token\"]").GetAttributeValue("value", string.Empty);
        }

        private void OnPostResponse(HttpWebRequest request, HttpWebResponse response)
        {
            var cookie = response.Cookies["session-production"];
            if (cookie != null)
            {
                SessionCookie = cookie;
            }
        }
    }
}
