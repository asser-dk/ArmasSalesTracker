namespace Asser.ArmasSalesTracker.Models
{
    using System;

    public class Price
    {
        public DateTime Timestamp { get; set; }

        public PriceTypes Type { get; set; }

        public int Value { get; set; }
    }
}
