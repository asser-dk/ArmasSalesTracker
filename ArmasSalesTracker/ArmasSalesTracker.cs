namespace Asser.ArmasSalesTracker
{
    using System;
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

        private readonly IProductLineService productService;

        private readonly ISubscriberService subscriberService;

        public ArmasSalesTracker(IArmasScraper scraper, IProductLineService productService, ISubscriberService subscriberService)
        {
            this.scraper = scraper;
            this.productService = productService;
            this.subscriberService = subscriberService;
        }

        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var kernel = new StandardKernel(new ArmasSalesTrackerModule());

            var tracker = new ArmasSalesTracker(
                kernel.Get<IArmasScraper>(),
                kernel.Get<IProductLineService>(),
                kernel.Get<ISubscriberService>());

            tracker.RunJob();

            Console.ReadKey();
        }

        public void RunJob()
        {
            Log.Info("Getting latest data from ARMAS");

            try
            {
                Log.Info("Get tabs");
                var pages = scraper.GetAllPages();

                Log.Info("Logging in as fremium");
                scraper.LogInAsFreemium();
                foreach (var pageInfo in pages)
                {
                    var products = scraper.GetProductAndFreemiumInfo(pageInfo);
                    foreach (var product in products)
                    {
                        productService.UpdateProductData(product);
                    }
                }

                Log.Info("Completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception during scraping", ex);
            }

            Log.Info("Done.");
        }

        private void SendAlerts(ProductLine product)
        {
            var normalPrices = productLineService.GetNormalPrices(product.Id);

            if (normalPrices.Price < product.Prices.Price || normalPrices.Premium < product.Prices.Premium)
            {
                subscriberService.SendAlerts(product, normalPrices);
            }
        }
    }
}
