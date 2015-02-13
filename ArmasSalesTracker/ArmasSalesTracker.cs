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

        private readonly IProductLineUpdater updater;

        public ArmasSalesTracker(IArmasScraper scraper, IProductLineUpdater updater)
        {
            this.scraper = scraper;
            this.updater = updater;
        }

        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var kernel = new StandardKernel(new ArmasSalesTrackerModule());

            var tracker = new ArmasSalesTracker(kernel.Get<IArmasScraper>(), kernel.Get<IProductLineUpdater>());

            tracker.GetLatestData();
        }

        private void GetLatestData()
        {
            Log.Info("Getting latest data from ARMAS");
            try
            {
                var products = scraper.GetArmasProductLines();
                updater.UpdateProductLines(products);
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                Console.WriteLine(ex);
            }

            Log.Info("Done.");
        }
    }
}
