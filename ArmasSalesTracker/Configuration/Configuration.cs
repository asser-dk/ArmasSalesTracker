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

        public string MySqlConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["MySql"].ConnectionString;
            }
        }

        public string ArmasUsername
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.Username"];
            }
        }

        public string ArmasPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.Password"];
            }
        }

        public string ArmasLoginPageUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.LoginPageUrl"];
            }
        }

        public string ArmasRegisterUrl
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.RegisterUrl"];
            }
        }

        public string ArmasPremiumUsername
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.Premium.Username"];
            }
        }

        public string ArmasPremiumPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["Armas.Premium.Password"];
            }
        }

        public string PostmarkServerToken
        {
            get
            {
                return ConfigurationManager.AppSettings["Postmark.ServerToken"];
            }
        }

        public string PremiumNotificationEmail
        {
            get
            {
                return ConfigurationManager.AppSettings["PremiumNotificationEmail"];
            }
        }

        public string AlertFromEmail
        {
            get
            {
                return ConfigurationManager.AppSettings["Alerts.FromEmail"];
            }
        }

        public string AlertReplyToEmail
        {
            get
            {
                return ConfigurationManager.AppSettings["Alerts.ReplyToEmail"];
            }
        }
    }
}
