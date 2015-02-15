namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface ISubscriberService : IDisposable
    {
        void SendAlerts(ProductLine product, ProductPrice normalPrices);
    }
}
