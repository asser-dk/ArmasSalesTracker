namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductService : IDisposable
    {
        void UpdateProductData(Product product);
    }
}
