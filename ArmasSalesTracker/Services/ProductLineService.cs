namespace Asser.ArmasSalesTracker.Services
{
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;

    public class ProductLineService : IProductLineService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MySqlConnection connection;

        private readonly MySqlCommand updateProductCommand;

        private readonly MySqlCommand latestPriceCommand;

        private readonly MySqlCommand insertPriceCommand;

        private readonly MySqlCommand updatePriceTimestampCommand;

        public ProductLineService(IConfiguration configuration)
        {
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            updateProductCommand =
                new MySqlCommand(
                    "INSERT INTO Product (Id, Url, ImageUrl, Title, Category) "
                    + "VALUES (@Id, @Url, @ImageUrl, @Title, @Category) "
                    + "ON DUPLICATE KEY UPDATE Url=VALUES(Url), ImageUrl=VALUES(ImageUrl), Title=VALUES(Title), Category=VALUES(Category)",
                    connection);

            latestPriceCommand =
                new MySqlCommand(
                    "SELECT Timestamp, Value FROM Price WHERE Product = @ProductId AND Type = @Type ORDER BY Timestamp DESC LIMIT 1",
                    connection);

            insertPriceCommand = new MySqlCommand("INSERT INTO Price (Product, Value, Type, Timestamp) VALUES (@ProductId, @Value, @Type, @Timestamp)", connection);

            updatePriceTimestampCommand =
                new MySqlCommand(
                    "UPDATE Price SET Timestamp = NOW() WHERE Product = @ProductId AND Type = @Type AND @Timestamp = @Timestamp AND Value = @Value LIMIT 1",
                    connection);

            updateProductCommand.Prepare();
            latestPriceCommand.Prepare();
            insertPriceCommand.Prepare();
            updatePriceTimestampCommand.Prepare();
        }

        public void Dispose()
        {
            updateProductCommand.Dispose();
            latestPriceCommand.Dispose();
            insertPriceCommand.Dispose();
            updateProductCommand.Dispose();
            connection.Close();
            connection.Dispose();
        }

        public void UpdateProductData(Product productLine)
        {
            Log.Info(string.Format("Updating product data for \"{0}\" (Id: {1})", productLine.Title, productLine.Id));
            updateProductCommand.Parameters.Clear();
            updateProductCommand.Parameters.AddWithValue("@Id", productLine.Id);
            updateProductCommand.Parameters.AddWithValue("@Url", productLine.Url);
            updateProductCommand.Parameters.AddWithValue("@ImageUrl", productLine.ImageUrl);
            updateProductCommand.Parameters.AddWithValue("@Title", productLine.Title);
            updateProductCommand.Parameters.AddWithValue("@Category", productLine.Category);
            updateProductCommand.ExecuteNonQuery();
        }

        public void UpdatePriceInfo(string productId, Price price)
        {
            var latestPrice = GetLatestPrice(productId, price.Type);

            if (latestPrice.Value != price.Value)
            {
                InsertPricePoint(productId, price);
            }
            else
            {
                UpdatePricePointTimestamp(productId, latestPrice);
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
            updatePriceTimestampCommand.Parameters.Clear();
            updatePriceTimestampCommand.Parameters.AddWithValue("@ProductId", productId);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Value", price.Value);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            updatePriceTimestampCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            updatePriceTimestampCommand.ExecuteNonQuery();
        }

        private void InsertPricePoint(string productId, Price price)
        {
            insertPriceCommand.Parameters.Clear();
            insertPriceCommand.Parameters.AddWithValue("@ProductId", productId);
            insertPriceCommand.Parameters.AddWithValue("@Value", price.Value);
            insertPriceCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            insertPriceCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            insertPriceCommand.ExecuteNonQuery();
        }
    }
}
