namespace Asser.ArmasSalesTracker.Models
{
    public class ProductLine
    {
        public ProductLine()
        {
            Prices = new ProductPrice();
        }

        public string Id { get; set; }

        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        public ProductPrice Prices { get; set; }
    }
}
