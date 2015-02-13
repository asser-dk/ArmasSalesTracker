namespace Asser.ArmasSalesTracker.Configuration
{
    public interface IConfiguration
    {
        string ArmasBaseHost { get; }

        string ArmasBaseUrl { get; }

        string ArmasFrontpagePageUri { get; }
    }
}
