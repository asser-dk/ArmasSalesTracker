namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface ISubscriberService : IDisposable
    {
        void SendAlerts(Product product);
    }
}
