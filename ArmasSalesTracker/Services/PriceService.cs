namespace Asser.ArmasSalesTracker.Services
{
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;

    public class PriceService : IPriceService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MySqlConnection connection;

        private readonly MySqlCommand latestPriceCommand;

        private readonly MySqlCommand insertPriceCommand;

        private readonly MySqlCommand updatePriceTimestampCommand;

        public PriceService(IConfiguration configuration)
        {
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            latestPriceCommand =
                new MySqlCommand(
                    "SELECT Timestamp, Value FROM Price WHERE Product = @ProductId AND Type = @Type ORDER BY Timestamp DESC LIMIT 1",
                    connection);

            insertPriceCommand = new MySqlCommand("INSERT INTO Price (Product, Value, Type, Timestamp) VALUES (@ProductId, @Value, @Type, @Timestamp)", connection);

            updatePriceTimestampCommand =
                new MySqlCommand(
                    "UPDATE Price SET Timestamp = NOW() WHERE Product = @ProductId AND Type = @Type AND @Timestamp = @Timestamp AND Value = @Value LIMIT 1",
                    connection);

            latestPriceCommand.Prepare();
            insertPriceCommand.Prepare();
            updatePriceTimestampCommand.Prepare();
        }

        public void Dispose()
        {
            Log.Debug("Disposing price service");
            latestPriceCommand.Dispose();
            insertPriceCommand.Dispose();
            updatePriceTimestampCommand.Dispose();

            connection.Close();
            connection.Dispose();
        }

        public void UpdatePriceInfo(string productId, Price price)
        {
            Log.Debug(string.Format("Update price point for {0} Type: {1}, value: {2}", productId, price.Type, price.Value));

            var latestPrice = GetLatestPrice(productId, price.Type);

            if (latestPrice == null)
            {
                Log.Debug("No latest price (new product?)");
                InsertPricePoint(productId, price);
            }
            else
            {
                Log.Debug(string.Format("Latest price was {0}", latestPrice.Value));
                if (latestPrice.Value != price.Value)
                {
                    InsertPricePoint(productId, price);
                }
                else
                {
                    UpdatePricePointTimestamp(productId, latestPrice);
                }
            }
        }

        public Price GetLatestPrice(string productId, PriceTypes type)
        {
            latestPriceCommand.Parameters.Clear();
            latestPriceCommand.Parameters.AddWithValue("@ProductId", productId);
            latestPriceCommand.Parameters.AddWithValue("@Type", (int)type);

            using (var reader = latestPriceCommand.ExecuteReader())
            {
                return reader.Read()
                           ? new Price
                           {
                               Timestamp = reader.GetDateTime("Timestamp"),
                               Value = reader.GetInt32("Value"),
                               Type = type
                           }
                           : null;
            }
        }

        private void UpdatePricePointTimestamp(string productId, Price price)
        {
            Log.Debug("Updating existing price timestamp");
            updatePriceTimestampCommand.Parameters.Clear();
            updatePriceTimestampCommand.Parameters.AddWithValue("@ProductId", productId);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Value", price.Value);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            updatePriceTimestampCommand.ExecuteNonQuery();
        }

        private void InsertPricePoint(string productId, Price price)
        {
            Log.Debug("Inserting new price point");
            insertPriceCommand.Parameters.Clear();
            insertPriceCommand.Parameters.AddWithValue("@ProductId", productId);
            insertPriceCommand.Parameters.AddWithValue("@Value", price.Value);
            insertPriceCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            insertPriceCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            insertPriceCommand.ExecuteNonQuery();
        }
    }
}
