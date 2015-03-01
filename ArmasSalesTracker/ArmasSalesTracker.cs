namespace Asser.ArmasSalesTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using Asser.ArmasSalesTracker.Services;
    using log4net;
    using log4net.Config;
    using Ninject;

    public class ArmasSalesTracker
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IArmasScraper scraper;

        private readonly IProductService productService;

        private readonly IPriceService priceService;

        private readonly ISubscriberService subscriberService;

        private readonly IPremiumNotifierService premiumNotifierService;

        public ArmasSalesTracker(
            IArmasScraper scraper,
            IProductService productService,
            IPriceService priceService,
            ISubscriberService subscriberService,
            IPremiumNotifierService premiumNotifierService)
        {
            this.scraper = scraper;
            this.productService = productService;
            this.priceService = priceService;
            this.subscriberService = subscriberService;
            this.premiumNotifierService = premiumNotifierService;
        }

        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var kernel = new StandardKernel(new ArmasSalesTrackerModule());

            var tracker = kernel.Get<ArmasSalesTracker>();

            tracker.RunJob();

            Console.ReadKey();
        }

        public void RunJob()
        {
            Log.Info("Getting latest data from ARMAS");

            var startTime = DateTime.UtcNow;

            try
            {
                Log.Info("Get tabs");
                var pages = scraper.GetAllPages().ToList();

                Log.Info("Logging in as freemium");
                scraper.LogInAsFreemium();
                foreach (var pageInfo in pages)
                {
                    var products = scraper.GetProductAndFreemiumInfo(pageInfo);
                    foreach (var product in products)
                    {
                        Log.Info(string.Format("Updating basic info, default and current price for \"{0}\" (Id: {1})", product.Title, product.Id));
                        productService.UpdateProductData(product);
                        foreach (var price in product.PriceInfo)
                        {
                            priceService.UpdatePriceInfo(product.Id, price);
                        }
                    }
                }

                Log.Info("Logging in as premium");
                scraper.LogInAsPremium();

                AlertIfNumberOfPremiumDaysAreLow();
                foreach (var pageInfo in pages)
                {
                    var premiumPrices = scraper.GetPremiumPrices(pageInfo);
                    foreach (var premiumPrice in premiumPrices)
                    {
                        priceService.UpdatePriceInfo(premiumPrice.ProductId, premiumPrice.Price);
                    }
                }

                Log.Info("Getting products on sale");
                var productsOnSale = GetProductsOnSale(startTime);
                Log.Info("Sending alerts");
                foreach (var product in productsOnSale)
                {
                    subscriberService.SendAlerts(product);
                }

                Log.Info("Completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception during scraping", ex);
            }

            Log.Info("Done.");
        }

        private void AlertIfNumberOfPremiumDaysAreLow()
        {
            var daysOfPremiumLeft = scraper.GetDaysOfPremiumLeft();
            Log.Info(daysOfPremiumLeft + " days of premium left.");

            if (daysOfPremiumLeft < 3)
            {
                premiumNotifierService.SendLowPremiumCountEmail(daysOfPremiumLeft);
            }
        }

        private IEnumerable<Product> GetProductsOnSale(DateTime startTime)
        {
            var products = productService.GetProducts();

            foreach (var product in products)
            {
                var defaultPrice = priceService.GetLatestPrice(product.Id, PriceTypes.Default);
                var currentPrice = priceService.GetLatestPrice(product.Id, PriceTypes.Current);
                var premiumPrice = priceService.GetLatestPrice(product.Id, PriceTypes.Premium);

                product.PriceInfo.Add(defaultPrice);
                product.PriceInfo.Add(currentPrice);
                product.PriceInfo.Add(premiumPrice);

                if (currentPrice.Timestamp >= startTime && currentPrice.Timestamp >= defaultPrice.Timestamp)
                {
                    if (currentPrice.Value < defaultPrice.Value)
                    {
                        yield return product;
                    }
                }

                if (premiumPrice.Timestamp > startTime && premiumPrice.Timestamp >= defaultPrice.Timestamp)
                {
                    if (premiumPrice.Value < (defaultPrice.Value * 0.8))
                    {
                        yield return product;
                    }
                }
            }
        }
    }
}
