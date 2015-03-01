namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using MySql.Data.MySqlClient;
    using PostmarkDotNet;

    public class SubscriberService : ISubscriberService
    {
        private readonly MySqlConnection connection;

        private readonly MySqlCommand selectSubscribersCommand;

        private readonly MySqlCommand deleteSubscribersCommand;

        private readonly PostmarkClient client;

        public SubscriberService(IConfiguration configuration)
        {
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

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }

        public void SendAlerts(Product product)
        {
            foreach (var subscriber in GetSubscribers(product.Id))
            {
                var current = product.PriceInfo.First(x => x.Type == PriceTypes.Current).Value;
                var dfault = product.PriceInfo.First(x => x.Type == PriceTypes.Default).Value;
                var premium = product.PriceInfo.First(x => x.Type == PriceTypes.Premium).Value;
                var premiumDefault = Math.Round(product.PriceInfo.First(x => x.Type == PriceTypes.Current).Value * 0.8);

                var discount = Math.Round((1 - ((double)current / dfault)) * 100);
                var premiumDiscount = Math.Round((1 - (premium / premiumDefault)) * 100);

                var mailMessage = new PostmarkMessage
                {
                    From = "apbsales@sexyfishhorse.com",
                    To = subscriber,
                    Subject = "[APB Sales] " + product.Title + " is now on sale at ARMAS",
                    ReplyTo = "no-reply@sexyfishhorse.com",
                    TrackOpens = true,
                    HtmlBody = GetHtmlBody(product, discount, premiumDiscount, current, dfault, premium, premiumDefault),
                    TextBody = GetTextBody(product, discount, premiumDiscount, current, dfault, premium, premiumDefault)
                };

                client.SendMessageAsync(mailMessage);
            }

            DeleteSubscribersForProduct(product);
        }

        private static string GetHtmlBody(Product product, double discount, double premiumDiscount, int current, int dfault, int premium, double premiumDefault)
        {
            string template;

            using (var reader = new StreamReader("MailTemplate.html"))
            {
                template = reader.ReadToEnd();

                template =
                    template.Replace("¤Title¤", product.Title)
                        .Replace("¤Url¤", product.Url)
                        .Replace("¤ImageUrl¤", product.ImageUrl)
                        .Replace("¤Normal.Price¤", dfault.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Latest.Price¤", current.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Normal.Discount¤", discount.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Normal.Premium¤", premium.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Latest.Premium¤", premiumDefault.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤Premium.Discount¤", premiumDiscount.ToString(CultureInfo.InvariantCulture))
                        .Replace("¤LastUpdated¤", DateTime.UtcNow.ToString("U"))
                        .Replace("¤Category¤", product.Category);
            }

            return template;
        }

        private static string GetTextBody(Product product, double discount, double premiumDiscount, int current, int dfault, int premium, double premiumDefault)
        {
            return string.Format(
                "{0} is on sale on ARMAS!\n\rCategory: {8}\n\r\n\rNormal price: {2} - Discounted at {3} - Save {4} %\n\rPremium normal price: {5} - Discounted at {6} - Save {7} %\n\r\n\rVisit ARMAS: {1}\n\r\n\rYou are receiving this mail because you signed up for an email alert the next time this product went on sale. You will only receive this email once for this product.\n\r\n\rBest regards SexyFishHorse Armas sales tracker\n\rhttp://apbsales.sexyfishhorse.com",
                product.Title,
                product.Url,
                dfault,
                current,
                discount,
                premiumDefault,
                premium,
                premiumDiscount,
                product.Category);
        }

        private void DeleteSubscribersForProduct(Product product)
        {
            deleteSubscribersCommand.Parameters.Clear();
            deleteSubscribersCommand.Parameters.AddWithValue("@ProductId", product.Id);
            deleteSubscribersCommand.ExecuteNonQuery();
        }

        private IEnumerable<string> GetSubscribers(string productId)
        {
            selectSubscribersCommand.Parameters.Clear();
            selectSubscribersCommand.Parameters.AddWithValue("@ProductId", productId);

            var result = selectSubscribersCommand.ExecuteReader();

            while (result.Read())
            {
                yield return result.GetString("Email");
            }
        }
    }
}
