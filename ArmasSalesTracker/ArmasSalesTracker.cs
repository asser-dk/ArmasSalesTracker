namespace Asser.ArmasSalesTracker
{
    using System;
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
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

        public ArmasSalesTracker(IArmasScraper scraper, IProductService productService, IPriceService priceService, ISubscriberService subscriberService)
        {
            this.scraper = scraper;
            this.productService = productService;
            this.priceService = priceService;
            this.subscriberService = subscriberService;
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

            try
            {
                Log.Info("Get tabs");
                var pages = scraper.GetAllPages();

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
                foreach (var pageInfo in pages)
                {
                    var premiumPrices = scraper.GetPremiumPrices(pageInfo);
                    foreach (var premiumPrice in premiumPrices)
                    {
                        priceService.UpdatePriceInfo(premiumPrice.ProductId, premiumPrice.Price);
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
    }
}
