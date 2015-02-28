namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Security.Authentication;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI.WebControls;
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

        public CookieCollection Cookies { get; set; }

        public HtmlDocument GetPageContent(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = GetCookies();
            request.Referer = url;
            request.Host = configuration.ArmasBaseHost.Replace("http://", string.Empty);

            var response = request.GetResponse();

            using (var output = response.GetResponseStream())
            {
                var document = new HtmlDocument();
                document.Load(output);

                return document;
            }
        }

        public IEnumerable<ProductLine> GetArmasProductLines()
        {
            Log.Info("Update all armas products");
            LogInToArmas(configuration.ArmasUsername, configuration.ArmasPassword);

            return
                GetTabs()
                    .SelectMany(GetSubPages, (tabInfo, page) => new { tabInfo, page })
                    .SelectMany(@t => GetProductLines(@t.page, @t.tabInfo));
        }

        public IEnumerable<PageInfo> GetTabs()
        {
            Log.Info("Acquiring tabs");
            var request = (HttpWebRequest)WebRequest.Create(configuration.ArmasFrontpagePageUri);
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";
            request.CookieContainer = GetCookies();

            using (var response = request.GetResponse())
            {
                var doc = new HtmlDocument();
                doc.Load(response.GetResponseStream());
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
        }

        public IEnumerable<PageInfo> GetSubPages(PageInfo tabInfo)
        {
            Log.Info(string.Format("Acquiring sub pages for  {0}", tabInfo.Title));
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
            Log.Info(string.Format("Get product lines for page {0}", pageInfo.Title));
            var doc = GetPageContent(pageInfo.Url);
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

                var priceNode =
                    productLineNode.SelectSingleNode(
                        "table/tr/td[@class='product_price_container']/span/b/span[@class='product_g1c_price']");

                productLine.Prices.Price =
                    int.Parse(priceNode.SelectSingleNode("text()").InnerText.Replace(" G1C", string.Empty));

                var defaultPriceNode = priceNode.SelectSingleNode("strike");

                if (defaultPriceNode != null)
                {
                    productLine.Prices.DefaultPrice = int.Parse(defaultPriceNode.InnerText);
                }
                else
                {
                    productLine.Prices.DefaultPrice = productLine.Prices.Price;
                }

                var premiumPriceNode =
                    productLineNode.SelectSingleNode("table/tr/td[@class='product_price_container']/span/b/span/span[contains(@class, 'premium_price')]");

                if (premiumPriceNode != null)
                {
                    productLine.Prices.Premium =
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
            Log.Info("Log in to armas");
            Log.Debug("Aquiring login token");
            var web = new HtmlWeb { UseCookies = true, PostResponse = OnPostResponse };
            var doc = web.Load(configuration.ArmasLoginPageUrl);
            var loginToken = doc.DocumentNode.SelectSingleNode("//*[@id=\"login__token\"]").GetAttributeValue("value", string.Empty);
            Log.Debug(string.Format("Login token: {0}", loginToken));

            var postData = new StringBuilder();
            postData.Append(string.Format("{0}={1}&", HttpUtility.UrlEncode("login[email]"), HttpUtility.UrlEncode(username)));
            postData.Append(string.Format("{0}={1}&", HttpUtility.UrlEncode("login[password]"), HttpUtility.UrlEncode(password)));
            postData.Append(string.Format("{0}={1}&", HttpUtility.UrlEncode("login[_token]"), HttpUtility.UrlEncode(loginToken)));
            postData.Append(string.Format("{0}={1}", HttpUtility.UrlEncode("_target_path"), HttpUtility.UrlEncode("http://gamersfirst.com")));

            var ascii = new ASCIIEncoding();
            var postBytes = ascii.GetBytes(postData.ToString());

            Log.Debug("Sending login request");

            var request = (HttpWebRequest)WebRequest.Create(configuration.ArmasLoginPageUrl);
            request.AllowAutoRedirect = false;
            request.Method = "POST";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;
            request.CookieContainer = GetCookies();
            request.Referer = configuration.ArmasLoginPageUrl;
            request.Host = configuration.ArmasRegisterUrl.Replace("https://", string.Empty);
            request.Headers.Add("Origin", configuration.ArmasRegisterUrl);

            using (var postStream = request.GetRequestStream())
            {
                postStream.Write(postBytes, 0, postBytes.Length);
            }

            Log.Debug("Processing login response");

            using (var response = request.GetResponse())
            {
                var responseCookies = ((HttpWebResponse)response).Cookies;
                foreach (var cookie in responseCookies)
                {
                    Cookies.Add((Cookie)cookie);
                }

                var redirectRequest = (HttpWebRequest)WebRequest.Create(response.ResponseUri);
                redirectRequest.CookieContainer = GetCookies();
                redirectRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/40.0.2214.115 Safari/537.36";

                using (var redirectResponse = redirectRequest.GetResponse())
                {
                    using (var output = redirectResponse.GetResponseStream())
                    {
                        if (output == null)
                        {
                            throw new AuthenticationException("Unable to log in to armas. No response stream given.");
                        }

                        using (var reader = new StreamReader(output))
                        {
                            var data = reader.ReadToEnd();

                            if (!data.Contains("<div class=\"g1c_balance_top_nav\">"))
                            {
                                throw new AuthenticationException("Unable to log in to armas. G1C credit counter not available.");
                            }

                            Log.Info("Successfully logged in to Armas");
                        }
                    }
                }
            }
        }

        private void OnPostResponse(HttpWebRequest request, HttpWebResponse response)
        {
            Cookies = new CookieCollection();

            foreach (var cookie in response.Cookies)
            {
                Cookies.Add((Cookie)cookie);
            }
        }

        private CookieContainer GetCookies()
        {
            var container = new CookieContainer();
            foreach (var cookie in Cookies)
            {
                container.Add((Cookie)cookie);
            }

            return container;
        }

        public IEnumerable<PageInfo> GetAllPages()
        {
            return GetTabs().SelectMany(GetSubPages);
        }
    }
}
