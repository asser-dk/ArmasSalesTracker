namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductLineService : IDisposable
    {
        void UpdateProductData(ProductLine product);

        ProductPrice GetNormalPrices(string productId);
    }
}
