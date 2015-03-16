namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;
    using PostmarkDotNet;

    public class SubscriberService : ISubscriberService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConfiguration configuration;

        private readonly MySqlConnection connection;

        private readonly MySqlCommand selectSubscribersCommand;

        private readonly MySqlCommand deleteSubscribersCommand;

        private readonly PostmarkClient client;

        public SubscriberService(IConfiguration configuration)
        {
            this.configuration = configuration;
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            selectSubscribersCommand = new MySqlCommand("SELECT Email FROM AlertSignup WHERE Product = @ProductId", connection);
            deleteSubscribersCommand = new MySqlCommand(
                "DELETE FROM AlertSignup WHERE Product = @ProductId",
                connection);

            selectSubscribersCommand.Prepare();
            deleteSubscribersCommand.Prepare();

            client = new PostmarkClient(configuration.PostmarkServerToken);
        }

        public int TotalAlertsSent { get; private set; }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public async Task SendAlerts(Product product)
        {
            var messages = new Collection<PostmarkMessage>();
            foreach (var subscriber in GetSubscribers(product.Id))
            {
                var current = product.PriceInfo.First(x => x.Type == PriceTypes.Current).Value;
                var dfault = product.PriceInfo.First(x => x.Type == PriceTypes.Default).Value;
                var discount = (int)Math.Round((1 - ((double)current / dfault)) * 100);

                var premiumCurrentRaw = product.PriceInfo.FirstOrDefault(x => x.Type == PriceTypes.CurrentPremium);
                var premium = 0;
                if (premiumCurrentRaw != null)
                {
                    premium = premiumCurrentRaw.Value;
                }

                int premiumDiscount = 0;
                if (premium != 0)
                {
                    premiumDiscount = (int)Math.Round((1 - ((double)premium / dfault)) * 100);
                }

                var mailMessage = new PostmarkMessage
                {
                    From = configuration.AlertFromEmail,
                    To = subscriber,
                    Subject = string.Format("[APB Sales] {0} is now on sale at ARMAS", product.Title),
                    ReplyTo = configuration.AlertReplyToEmail,
                    TrackOpens = true,
                    HtmlBody = GetHtmlBody(product, discount, premiumDiscount, current, dfault, premium),
                    TextBody = GetTextBody(product, discount, premiumDiscount, current, dfault, premium)
                };

                messages.Add(mailMessage);
            }

            var sendTask = client.SendMessagesAsync(messages);
            var deleteTask = DeleteSubscribersForProduct(product);

            Log.Debug(string.Format("Sent {0} alerts for the product {1} (Id {2})", messages.Count, product.Title, product.Id));
            TotalAlertsSent += messages.Count;

            await sendTask;
            await deleteTask;
        }

        private static string GetHtmlBody(Product product, double discount, double premiumDiscount, int current, int dfault, int premium)
        {
            var file = premium != 0 ? "MailTemplate.html" : "MailTemplateNoPremium.html";

            using (var reader = new StreamReader(file))
            {
                var template = reader.ReadToEnd();

                return template.Replace("¤Title¤", product.Title)
                        .Replace("¤Url¤", product.Url)
                        .Replace("¤ImageUrl¤", product.ImageUrl)
                        .Replace("¤Normal.Price¤", dfault.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Latest.Price¤", current.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Normal.Discount¤", discount.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Normal.Premium¤", premium.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Premium.Discount¤", premiumDiscount.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤LastUpdated¤", DateTime.UtcNow.ToString("U"))
                        .Replace("¤Category¤", product.Category);
            }
        }

        private static string GetTextBody(Product product, int discount, int premiumDiscount, int current, int dfault, int premium)
        {
            if (premiumDiscount != 0 && premiumDiscount != 20)
            {
                return
                    string.Format(
                        "{0} is on sale on ARMAS!\n\rCategory: {7}\n\r\n\rNormal price: {2} - Discounted at {3} - Save {4} %\n\rPremium price: {5} - Save {6} %\n\r\n\rVisit ARMAS: {1}\n\r\n\rYou are receiving this mail because you signed up for an email alert the next time this product went on sale. You will only receive this email once for this product.\n\r\n\rBest regards SexyFishHorse Armas sales tracker\n\rhttp://apbsales.sexyfishhorse.com",
                        product.Title,
                        product.Url,
                        dfault,
                        current,
                        discount,
                        premium,
                        premiumDiscount,
                        product.Category);
            }

            return
                string.Format(
                    "{0} is on sale on ARMAS!\n\rCategory: {5}\n\r\n\rNormal price: {2} - Discounted at {3} - Save {4} %\n\r\n\rVisit ARMAS: {1}\n\r\n\rYou are receiving this mail because you signed up for an email alert the next time this product went on sale. You will only receive this email once for this product.\n\r\n\rBest regards SexyFishHorse Armas sales tracker\n\rhttp://apbsales.sexyfishhorse.com",
                    product.Title,
                    product.Url,
                    dfault,
                    current,
                    discount,
                    product.Category);
        }

        private async Task DeleteSubscribersForProduct(Product product)
        {
            deleteSubscribersCommand.Parameters.Clear();
            deleteSubscribersCommand.Parameters.AddWithValue("@ProductId", product.Id);
            await deleteSubscribersCommand.ExecuteNonQueryAsync();
        }

        private IEnumerable<string> GetSubscribers(string productId)
        {
            selectSubscribersCommand.Parameters.Clear();
            selectSubscribersCommand.Parameters.AddWithValue("@ProductId", productId);

            using (var result = selectSubscribersCommand.ExecuteReader())
            {
                while (result.Read())
                {
                    yield return result.GetString("Email");
                }
            }
        }
    }
}
