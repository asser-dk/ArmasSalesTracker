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

        private readonly bool isRunning;

        public ArmasSalesTracker(IArmasScraper scraper)
        {
            this.scraper = scraper;
            isRunning = true;
        }

        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            var kernel = new StandardKernel(new ArmasSalesTrackerModule());

            var tracker = new ArmasSalesTracker(kernel.Get<IArmasScraper>());
            tracker.StartTrackingLoop();
        }

        public void StartTrackingLoop()
        {
            Log.Info("Start tracking loop");
            while (isRunning)
            {
                try
                {
                    var products = scraper.GetArmasProductLines();

                    foreach (var productLine in products)
                    {
                        Console.WriteLine(productLine.Title + "(" + productLine.Id + ")");
                        Console.WriteLine("  Price: " + productLine.Price);
                        Console.WriteLine("  Premium price: " + productLine.PremiumPrice);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Caught exception in tracking loop", ex);
                    Console.WriteLine(ex);
                }
            }

            Log.Info("Ended tracking loop");
        }
    }
}
