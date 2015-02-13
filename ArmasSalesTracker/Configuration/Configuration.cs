namespace Asser.ArmasSalesTracker.Configuration
{
    using System.Configuration;

    public class Configuration : IConfiguration
    {
        public string ArmasBaseHost
        {
            get
            {
                return ConfigurationManager.AppSettings["ArmasHost"];
            }
        }

        public string ArmasBaseUrl
        {
            get
            {
                return ArmasBaseHost + ConfigurationManager.AppSettings["ArmasBaseUri"];
            }
        }

        public string ArmasFrontpagePageUri
        {
            get
            {
                return ArmasBaseUrl + ConfigurationManager.AppSettings["ArmasFrontpagePageUri"];
            }
        }
    }
}
