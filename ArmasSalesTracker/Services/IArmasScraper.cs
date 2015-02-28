namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using Asser.ArmasSalesTracker.Models;

    public interface IArmasScraper
    {
        IEnumerable<PageInfo> GetAllPages();

        void LogInAsFreemium();

        IEnumerable<Product> GetProductAndFreemiumInfo(PageInfo pageInfo);

        void LogInAsPremium();
    }
}
