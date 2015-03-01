namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Threading.Tasks;
    using Asser.ArmasSalesTracker.Models;

    public interface ISubscriberService : IDisposable
    {
        Task SendAlerts(Product product);
    }
}
