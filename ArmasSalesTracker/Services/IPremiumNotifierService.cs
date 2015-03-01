namespace Asser.ArmasSalesTracker.Services
{
    using System;

    public interface IPremiumNotifierService : IDisposable
    {
        void SendLowPremiumCountEmail(int daysOfPremiumLeft);
    }
}
