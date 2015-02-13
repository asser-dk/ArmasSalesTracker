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
    }
}
