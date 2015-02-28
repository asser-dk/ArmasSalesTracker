namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface IPriceService : IDisposable
    {
        void UpdatePriceInfo(string productId, Price price);

        Price GetLatestPrice(string productId, PriceTypes type);
    }
}