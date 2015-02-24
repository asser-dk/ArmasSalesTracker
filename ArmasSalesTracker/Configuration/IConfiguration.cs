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
    }
}
