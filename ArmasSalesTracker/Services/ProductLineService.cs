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

        private readonly MySqlCommand latestPricePointCommand;

        private readonly MySqlCommand insertPricePointCommand;

        private readonly MySqlCommand updatePricePointTimestampCommand;

        private readonly MySqlCommand insertProductPriceCommand;

        private readonly MySqlCommand normalPriceCommand;

        private readonly MySqlCommand latestPriceCommand;

        private readonly MySqlCommand updateProductPriceCommand;

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

            latestPricePointCommand =
                new MySqlCommand(
                    "SELECT Timestamp, Value FROM Price WHERE Product = @ProductId AND Type = @Type ORDER BY Timestamp DESC LIMIT 1",
                    connection);

            insertPricePointCommand = new MySqlCommand("INSERT INTO Price (Product, Value, Type, Timestamp) VALUES (@ProductId, @Value, @Type, @Timestamp)", connection);

            updatePricePointTimestampCommand =
                new MySqlCommand(
                    "UPDATE Price SET Timestamp = NOW() WHERE Product = @ProductId AND Type = @Type AND @Timestamp = @Timestamp AND Value = @Value LIMIT 1",
                    connection);

            insertProductPriceCommand = new MySqlCommand(
                "INSERT INTO ProductPrice (Product, Price, PremiumPrice, Timestamp, DefaultPrice) VALUES (@Product, @Price, @PremiumPrice, @Timestamp, @DefaultPrice)",
                connection);
            normalPriceCommand =
                new MySqlCommand(
                    "SELECT Price, PremiumPrice, DefaultPrice from ProductPrice WHERE Product = @ProductId ORDER BY Timestamp DESC LIMIT 1",
                    connection);
            latestPriceCommand = new MySqlCommand("SELECT Price, PremiumPrice, DefaultPrice, Timestamp FROM ProductPrice WHERE Product = @ProductId ORDER BY Timestamp DESC LIMIT 1", connection);
            updateProductPriceCommand = new MySqlCommand("UPDATE ProductPrice SET Timestamp = NOW() WHERE Product = @ProductId AND Timestamp = @Timestamp LIMIT 1", connection);

            updateProductCommand.Prepare();
            insertProductPriceCommand.Prepare();
            normalPriceCommand.Prepare();
            updateProductPriceCommand.Prepare();
            latestPriceCommand.Prepare();
        }

        public void InsertProductPrice(ProductLine productLine)
        {
            insertProductPriceCommand.Parameters.Clear();
            insertProductPriceCommand.Parameters.AddWithValue("@Product", productLine.Id);
            insertProductPriceCommand.Parameters.AddWithValue("@Price", productLine.Prices.Price);
            insertProductPriceCommand.Parameters.AddWithValue("@PremiumPrice", productLine.Prices.Premium);
            insertProductPriceCommand.Parameters.AddWithValue("@DefaultPrice", productLine.Prices.DefaultPrice);
            insertProductPriceCommand.ExecuteNonQuery();
        }

        public void UpdateLatestPrice(ProductLine productLine)
        {
            latestPriceCommand.Parameters.Clear();
            latestPriceCommand.Parameters.AddWithValue("@ProductId", productLine.Id);

            var reader = latestPriceCommand.ExecuteReader();
            ProductPrice latest = null;
            if (reader.HasRows)
            {
                reader.Read();
                latest = new ProductPrice
                {
                    Price = reader.GetInt32("Price"),
                    Premium = reader.GetInt32("PremiumPrice"),
                    DefaultPrice = reader.GetInt32("DefaultPrice"),
                    Timestamp = reader.GetDateTime("Timestamp")
                };
            }

            reader.Close();

            if (latest != null && productLine.Prices.Equals(latest))
            {
                Log.Debug("Same prices, updating old record with new timestamp");

                updateProductPriceCommand.Parameters.Clear();
                updateProductPriceCommand.Parameters.AddWithValue("@ProductId", productLine.Id);
                updateProductPriceCommand.Parameters.AddWithValue("@Timestamp", latest.Timestamp);
                updateProductPriceCommand.ExecuteNonQuery();
            }
            else
            {
                Log.Debug("Inserting new product price line");

                InsertProductPrice(productLine);
            }
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

        private void UpdatePricePointTimestamp(string productId, Price price)
        {
            updatePricePointTimestampCommand.Parameters.Clear();
            updatePricePointTimestampCommand.Parameters.AddWithValue("@ProductId", productId);
            updatePricePointTimestampCommand.Parameters.AddWithValue("@Value", price.Value);
            updatePricePointTimestampCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            updatePricePointTimestampCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            updatePricePointTimestampCommand.ExecuteNonQuery();
        }

        private void InsertPricePoint(string productId, Price price)
        {
            insertPricePointCommand.Parameters.Clear();
            insertPricePointCommand.Parameters.AddWithValue("@ProductId", productId);
            insertPricePointCommand.Parameters.AddWithValue("@Value", price.Value);
            insertPricePointCommand.Parameters.AddWithValue("@Type", (int)price.Type);
            insertPricePointCommand.Parameters.AddWithValue("@Timestamp", price.Timestamp);
            insertPricePointCommand.ExecuteNonQuery();
        }

        public Price GetLatestPrice(string productId, PriceTypes type)
        {
            latestPricePointCommand.Parameters.Clear();
            latestPricePointCommand.Parameters.AddWithValue("@ProductId", productId);
            latestPricePointCommand.Parameters.AddWithValue("@Type", (int)type);

            using (var reader = latestPricePointCommand.ExecuteReader())
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

        public ProductPrice GetNormalPrices(string productId)
        {
            try
            {
                normalPriceCommand.Parameters.Clear();
                normalPriceCommand.Parameters.AddWithValue("@ProductId", productId);
                var results = normalPriceCommand.ExecuteReader();
                results.Read();

                var productPrice = new ProductPrice
                {
                    Price = results.GetInt32("Price"),
                    Premium = results.GetInt32("PremiumPrice"),
                    DefaultPrice = results.GetInt32("DefaultPrice")
                };

                results.Close();

                return productPrice;
            }
            catch (MySqlException ex)
            {
                Log.Error(string.Format("Error trying to get normal price for product {0}", productId), ex);

                throw;
            }
        }

        public void Dispose()
        {
            insertProductPriceCommand.Dispose();
            updateProductCommand.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}
