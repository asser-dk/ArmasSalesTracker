namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductLineService : IDisposable
    {
        void UpdateProductLine(ProductLine product);

        ProductPrice GetNormalPrices(string productId);
    }
}
