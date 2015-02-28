namespace Asser.ArmasSalesTracker.Models
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class Product
    {
        public Product()
        {
            PriceInfo = new Collection<Price>();
        }

        public string Id { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public ICollection<Price> PriceInfo { get; set; }
    }
}
