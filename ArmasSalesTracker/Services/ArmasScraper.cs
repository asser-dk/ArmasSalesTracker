namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using HtmlAgilityPack;

    public class ArmasScraper
    {
        private readonly IConfiguration configuration;

        private readonly Regex tabTitleRegex = new Regex(@"\bnav_\w+\b");

        public ArmasScraper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public IEnumerable<PageInfo> GetTabs()
        {
            var web = new HtmlWeb();
            var doc = web.Load(configuration.ArmasFrontpagePageUri);
            var tabLinksNode = doc.DocumentNode.SelectNodes("//div[@id='product_categories_g1c']//a[@href]");

            foreach (var tabLink in tabLinksNode)
            {
                var classes = tabLink.Attributes["class"].Value;
                var matches = tabTitleRegex.Matches(classes);
                var title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(matches[0].Value.Substring(4).Replace('_', ' '));
                yield return new PageInfo
                {
                    Title = title,
                    Url = configuration.ArmasBaseHost + tabLink.Attributes["href"].Value
                };
            }
        }

        public IEnumerable<PageInfo> GetSubPages(string pageUrl)
        {
            var web = new HtmlWeb();
            var doc = web.Load(pageUrl);

            var subPagesNode = doc.DocumentNode.SelectNodes("//div[@id='product_subcategories_g1c']/ul/li");

            foreach (var subPageNode in subPagesNode)
            {
                var pageInfo = new PageInfo();

                var anchorNode = subPageNode.SelectSingleNode("a");

                if (anchorNode != null)
                {
                    pageInfo.Title = anchorNode.InnerText;
                    pageInfo.Url = configuration.ArmasBaseHost + "/" + anchorNode.Attributes["href"].Value;
                }
                else
                {
                    pageInfo.Url = pageUrl;
                    pageInfo.Title = subPageNode.InnerText;
                }

                yield return pageInfo;
            }
        }

        public IEnumerable<ProductLine> GetProductLines(string pageUrl)
        {
            var web = new HtmlWeb();
            var doc = web.Load(pageUrl);
            var productLinksNode = doc.DocumentNode.SelectNodes("//div[@id='products']/div/div");

            foreach (var productLineNode in productLinksNode)
            {
                var productLine = new ProductLine();

                productLine.Id = productLineNode.Id.Substring(7);

                productLine.Url = configuration.ArmasBaseUrl + "/"
                                  + productLineNode.SelectSingleNode("//td[@class='product_image_container']//a")
                                        .Attributes["href"].Value;

                productLine.ImageUrl = configuration.ArmasBaseUrl + productLineNode.SelectSingleNode("table/tr/td/a/img").Attributes["src"].Value;

                productLine.Title = productLineNode.SelectSingleNode("h4").InnerText;

                productLine.Price =
                    int.Parse(productLineNode.SelectSingleNode("//span[@class='product_g1c_price']").InnerText.Replace(" G1C", string.Empty));

                var premiumPriceNode =
                    productLineNode.SelectSingleNode("//span[@class[starts-with(., 'premium_price')]]");

                if (premiumPriceNode != null)
                {
                    productLine.PremiumPrice =
                    int.Parse(
                        premiumPriceNode
                            .InnerText.Replace(" G1C", string.Empty));
                }

                yield return productLine;
            }
        }
    }
}
