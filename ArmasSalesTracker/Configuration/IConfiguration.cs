namespace Asser.ArmasSalesTracker.Configuration
{
    public interface IConfiguration
    {
        string ArmasBaseHost { get; }

        string ArmasBaseUrl { get; }

        string ArmasFrontpagePageUri { get; }

        string MySqlConnectionString { get; }

        string ArmasUsername { get; }

        string ArmasPassword { get; }

        string ArmasLoginPageUrl { get; }

        string ArmasRegisterUrl { get; }

        string ArmasPremiumUsername { get; }

        string ArmasPremiumPassword { get; }

        string PostmarkServerToken { get; }

        string PremiumNotificationEmail { get; }
    }
}
