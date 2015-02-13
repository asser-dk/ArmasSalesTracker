namespace Asser.ArmasSalesTracker
{
    using System;
    using System.Linq;
    using Asser.ArmasSalesTracker.Services;

    public class ArmasSalesTracker
    {
        private readonly ArmasScraper scraper;

        public ArmasSalesTracker(ArmasScraper scraper)
        {
            this.scraper = scraper;
        }

        public static void Main(string[] args)
        {
            var scraper = new ArmasScraper(new Configuration.Configuration());

            var tracker = new ArmasSalesTracker(scraper);

            tracker.StartTrackingLoop();
        }

        public void StartTrackingLoop()
        {
            var tabLinks = scraper.GetTabs().Skip(1);

            foreach (var tabLink in tabLinks)
            {
                Console.WriteLine("Scraping tab: " + tabLink.Title);
            }
        }
    }
}
