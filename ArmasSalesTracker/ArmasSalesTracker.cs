namespace Asser.ArmasSalesTracker
{
    using System;
    using Asser.ArmasSalesTracker.Services;

    public class ArmasSalesTracker
    {
        private readonly IArmasScraper scraper;

        private readonly bool isRunning;

        public ArmasSalesTracker(IArmasScraper scraper)
        {
            this.scraper = scraper;

            isRunning = true;
        }

        public static void Main(string[] args)
        {
            var scraper = new ArmasScraper(new Configuration.Configuration());

            var tracker = new ArmasSalesTracker(scraper);

            tracker.StartTrackingLoop();
        }

        public void StartTrackingLoop()
        {
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

                    if (Console.ReadLine() != null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
