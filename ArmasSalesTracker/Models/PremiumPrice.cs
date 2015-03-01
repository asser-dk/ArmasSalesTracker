namespace Asser.ArmasSalesTracker.Models
{
    public class PremiumPrice
    {
        public string ProductId { get; set; }

        public Price Current { get; set; }

        public Price Default { get; set; }
    }
}
