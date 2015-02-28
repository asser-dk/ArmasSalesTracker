namespace Asser.ArmasSalesTracker.Models
{
    using System;

    public class ProductPrice
    {
        public int Price { get; set; }

        public int Premium { get; set; }

        public int DefaultPrice { get; set; }

        public DateTime Timestamp { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ProductPrice)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Premium;
                hashCode = (hashCode * 397) ^ Price;
                hashCode = (hashCode * 397) ^ DefaultPrice;
                return hashCode;
            }
        }

        protected bool Equals(ProductPrice other)
        {
            return Premium == other.Premium && Price == other.Price && DefaultPrice == other.DefaultPrice;
        }
    }
}