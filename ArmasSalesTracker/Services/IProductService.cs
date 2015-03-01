namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Collections.Generic;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductService : IDisposable
    {
        void UpdateProductData(Product product);

        IEnumerable<Product> GetProducts();

        void ClearFrontpage();

        void AddToFrontpage(Product product);
    }
}
