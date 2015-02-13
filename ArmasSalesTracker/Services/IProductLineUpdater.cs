namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using Asser.ArmasSalesTracker.Models;

    public interface IProductLineUpdater
    {
        void UpdateProductLines(IEnumerable<ProductLine> productLines);
    }
}
