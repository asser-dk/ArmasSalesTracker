﻿namespace Asser.ArmasSalesTracker.Services
{
    using System;
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

        public IEnumerable<TabInfo> GetTabs()
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
                    var title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(matches[0].Value.Substring(4).Replace('_', ' ')).Trim();
                    var url = configuration.ArmasBaseHost + tabLink.Attributes["href"].Value;

                    Log.Debug(string.Format("Found tab {0} with the link {1}", title, url));
                    yield return new TabInfo
                    {
                        Title = title,
                        Url = url
                    };
                }
            }
        }

        public IEnumerable<PageInfo> GetSubPages(TabInfo tabInfo)
        {
            Log.Info(string.Format("Acquiring sub pages for {0}", tabInfo.Title));
            var web = new HtmlWeb();
            var doc = web.Load(tabInfo.Url);

            var subPagesNode = doc.DocumentNode.SelectNodes("//div[@id='product_subcategories_g1c']/ul/li");

            foreach (var subPageNode in subPagesNode)
            {
                var pageInfo = new PageInfo();

                var anchorNode = subPageNode.SelectSingleNode("a");

                if (anchorNode != null)
                {
                    pageInfo.Title = anchorNode.InnerText.Trim();
                    pageInfo.Url = string.Format("{0}/{1}", configuration.ArmasBaseHost, anchorNode.Attributes["href"].Value);
                }
                else
                {
                    pageInfo.Url = tabInfo.Url;
                    pageInfo.Title = subPageNode.InnerText.Trim();
                }

                pageInfo.Parent = tabInfo;

                Log.Debug(string.Format("Found the subpage {0} with the link {1}", pageInfo.Title, pageInfo.Url));

                yield return pageInfo;
            }
        }

        public IEnumerable<PageInfo> GetAllPages()
        {
            return GetTabs().Skip(1).SelectMany(GetSubPages);
        }

        public void LogInAsFreemium()
        {
            LogInToArmas(configuration.ArmasUsername, configuration.ArmasPassword);
        }

        public IEnumerable<Product> GetProductAndFreemiumInfo(PageInfo pageInfo)
        {
            var document = GetPageContent(pageInfo.Url);

            var productsNode = document.DocumentNode.SelectNodes("//div[@id='products']/div/div[starts-with(@class, 'product_listing')]");

            foreach (var productNode in productsNode)
            {
                var product = GetProductData(productNode, pageInfo);
                var defaultPrice = GetDefaultPrice(productNode);
                var discount = GetCurrentPrice(productNode);

                product.PriceInfo.Add(defaultPrice);
                product.PriceInfo.Add(discount);

                yield return product;
            }
        }

        public void LogInAsPremium()
        {
            LogInToArmas(configuration.ArmasPremiumUsername, configuration.ArmasPremiumPassword);
        }

        public IEnumerable<PremiumPrice> GetPremiumPrices(PageInfo pageInfo)
        {
            var document = GetPageContent(pageInfo.Url);
            var productsNode = document.DocumentNode.SelectNodes("//div[@id='products']/div/div[starts-with(@class, 'product_listing')]");

            foreach (var productNode in productsNode)
            {
                var premiumPrice = new PremiumPrice
                {
                    ProductId = productNode.Id.Substring(7)
                };

                premiumPrice.Current = GetCurrentPrice(productNode);
                premiumPrice.Current.Type = PriceTypes.CurrentPremium;

                Log.Debug(
                    string.Format(
                        "Premium price {0} for the product {1}",
                        premiumPrice.ProductId,
                        premiumPrice.Current.Value));

                yield return premiumPrice;
            }
        }

        public int GetDaysOfPremiumLeft()
        {
            var doc = GetPageContent(configuration.ArmasFrontpagePageUri);

            var premiumCounter = doc.DocumentNode.SelectSingleNode("//*[@id='store_header_content']/div[1]/div/b").InnerText.Split(' ');

            return int.Parse(premiumCounter[0]);
        }

        private static Price GetCurrentPrice(HtmlNode productNode)
        {
            var priceNode = productNode.SelectSingleNode("table/tr/td[@class='product_price_container']/span/b/span[@class='product_g1c_price']/text()");

            var price = new Price
            {
                Type = PriceTypes.Current,
                Timestamp = DateTime.UtcNow,
                Value = int.Parse(priceNode.InnerText.Replace(" G1C", string.Empty))
            };

            return price;
        }

        private static Price GetDefaultPrice(HtmlNode productNode)
        {
            var priceNode = productNode.SelectSingleNode("table/tr/td[@class='product_price_container']/span/b/span[@class='product_g1c_price']");

            var price = new Price
            {
                Type = PriceTypes.Default,
                Timestamp = DateTime.UtcNow,
                Value = int.Parse(priceNode.SelectSingleNode("text()").InnerText.Replace(" G1C", string.Empty))
            };

            var defaultPriceNode = priceNode.SelectSingleNode("strike");

            if (defaultPriceNode != null)
            {
                price.Value = int.Parse(defaultPriceNode.InnerText);
            }

            return price;
        }

        private Product GetProductData(HtmlNode productNode, PageInfo pageInfo)
        {
            var product = new Product
            {
                Id = productNode.Id.Substring(7),
                Title = productNode.SelectSingleNode("h4").InnerText,
                Category = string.Format("{0} - {1}", pageInfo.Parent.Title, pageInfo.Title),
            };

            product.ImageUrl = configuration.ArmasBaseHost + productNode.SelectSingleNode("table/tr/td/a/img").Attributes["src"].Value;

            product.Url = string.Format(
                "{0}/{1}",
                configuration.ArmasBaseUrl,
                productNode.SelectSingleNode("table/tr/td[@class='product_image_container']/a").Attributes["href"].Value);

            Log.Debug(string.Format("Found the product {0} (id: {1}) with the link {2}", product.Title, product.Id, product.Url));

            return product;
        }

        private void LogInToArmas(string username, string password)
        {
            Log.Debug("Clearing cookies");
            Cookies = new CookieCollection();

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
            if (Cookies != null)
            {
                foreach (var cookie in Cookies)
                {
                    container.Add((Cookie)cookie);
                }
            }

            return container;
        }
    }
}
