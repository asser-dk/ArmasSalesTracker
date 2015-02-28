namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using Asser.ArmasSalesTracker.Models;

    public interface IArmasScraper
    {
        IEnumerable<ProductLine> GetArmasProductLines();
        IEnumerable<PageInfo> GetAllPages();
    }
}
