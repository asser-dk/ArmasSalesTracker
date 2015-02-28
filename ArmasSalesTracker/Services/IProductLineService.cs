namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductLineService : IDisposable
    {
        void UpdateProductData(Product product);

        void UpdatePriceInfo(string product, Price price);
    }
}
