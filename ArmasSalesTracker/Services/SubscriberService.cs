namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
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

        public void SendAlerts(ProductLine product, ProductPrice normalPrices)
        {
            var subscribers = GetSubscribers(product.Id);

            foreach (var subscriber in subscribers)
            {
                var discount = Math.Round((1 - ((double)product.Prices.Price / normalPrices.Price)) * 100);
                var premiumDiscount = Math.Round((1 - ((double)product.Prices.Premium / normalPrices.Premium)) * 100);

                var mailMessage = new PostmarkMessage
                {
                    From = "apbsales@sexyfishhorse.com",
                    To = subscriber,
                    Subject = "[APB Sales] " + product.Title + " is now on sale at ARMAS",
                    ReplyTo = "narnia@sexyfishhorse.com",
                    TrackOpens = true
                };

                string template;

                using (var reader = new StreamReader("MailTemplate.html"))
                {
                    template = reader.ReadToEnd();

                    template =
                        template.Replace("¤Title¤", product.Title)
                            .Replace("¤Url¤", product.Url)
                            .Replace("¤ImageUrl¤", product.ImageUrl)
                            .Replace("¤Normal.Price¤", normalPrices.Price.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤Latest.Price¤", product.Prices.Price.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤Normal.Discount¤", discount.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤Normal.Premium¤", normalPrices.Premium.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤Latest.Premium¤", product.Prices.Premium.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤Premium.Discount¤", premiumDiscount.ToString(CultureInfo.InvariantCulture))
                            .Replace("¤LastUpdated¤", DateTime.UtcNow.ToString("U"))
                            .Replace("¤Category¤", product.Category);
                }

                mailMessage.HtmlBody = template;
                mailMessage.TextBody =
                    string.Format(
                        "{0} is on sale on ARMAS!\n\rCategory: {8}\n\r\n\rNormal price: {2} - Discounted at {3} - Save {4} %\n\rPremium normal price: {5} - Discounted at {6} - Save {7} %\n\r\n\rVisit ARMAS: {1}\n\r\n\rYou are receiving this mail because you signed up for an email alert the next time this product went on sale. You will only receive this email once for this product.\n\r\n\rBest regards SexyFishHorse Armas sales tracker\n\rhttp://apbsales.sexyfishhorse.com",
                        product.Title,
                        product.Url,
                        normalPrices.Price,
                        product.Prices.Price,
                        discount,
                        normalPrices.Premium,
                        product.Prices.Premium,
                        premiumDiscount,
                        product.Category);

                client.SendMessageAsync(mailMessage);
            }

            deleteSubscribersCommand.Parameters.Clear();
            deleteSubscribersCommand.Parameters.AddWithValue("@ProductId", product.Id);
            deleteSubscribersCommand.ExecuteNonQuery();
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
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
