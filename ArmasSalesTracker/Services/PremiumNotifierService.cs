namespace Asser.ArmasSalesTracker.Services
{
    using Asser.ArmasSalesTracker.Configuration;
    using MySql.Data.MySqlClient;
    using PostmarkDotNet;

    public class PremiumNotifierService : IPremiumNotifierService
    {
        private readonly IConfiguration configuration;

        private readonly MySqlConnection connection;

        private readonly PostmarkClient client;

        private readonly int threshold;

        private readonly int daysRemainingOnLastNotification;

        public PremiumNotifierService(IConfiguration configuration)
        {
            this.configuration = configuration;
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            using (
                var optionsCommand =
                    new MySqlCommand("SELECT MinDaysOfPremium, DaysRemainingOnLatestNotification FROM Options LIMIT 1", connection))
            {
                optionsCommand.Prepare();
                using (var reader = optionsCommand.ExecuteReader())
                {
                    reader.Read();

                    threshold = reader.GetInt32("MinDaysOfPremium");
                    daysRemainingOnLastNotification = reader.GetInt16("DaysRemainingOnLatestNotification");
                }
            }

            client = new PostmarkClient(configuration.PostmarkServerToken);
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public void SendLowPremiumCountEmail(int daysOfPremiumLeft)
        {
            if (daysOfPremiumLeft < threshold && daysOfPremiumLeft < daysRemainingOnLastNotification)
            {
                var mailMessage = new PostmarkMessage
                {
                    From = "apbsales@sexyfishhorse.com",
                    To = configuration.PremiumNotificationEmail,
                    Subject = string.Format("[APB Sales] Only {0} days of premium left!", daysOfPremiumLeft),
                    ReplyTo = "no-reply@sexyfishhorse.com",
                    TrackOpens = true,
                    TextBody =
                        string.Format("The armas sales tracker only has {0} days of premium left!", daysOfPremiumLeft)
                };

                client.SendMessageAsync(mailMessage);
            }
            else
            {
                using (var command = new MySqlCommand("UPDATE Options SET DaysRemainingOnLatestNotification = @Value", connection))
                {
                    command.Prepare();
                    command.Parameters.AddWithValue("@Value", daysOfPremiumLeft);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
