namespace ArmasSalesTrackerWebJob
{
    using Asser.ArmasSalesTracker;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Services;
    using Microsoft.Azure.WebJobs;
    using Ninject;

    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        public static void Main()
        {
            var host = new JobHost();

            host.Start();
            var kernel = new StandardKernel(new ArmasSalesTrackerModule());

            var tracker = new ArmasSalesTracker(
                kernel.Get<IArmasScraper>(),
                kernel.Get<IProductService>(),
                kernel.Get<ISubscriberService>());

            tracker.RunJob();
            host.Stop();
        }
    }
}
