namespace Asser.ArmasSalesTracker.Services
{
    using System;
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;

    public class ProductLineService : IProductLineService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MySqlConnection connection;

        private readonly MySqlCommand updateProductLineCommand;

        private readonly MySqlCommand insertProductPriceCommand;

        private readonly MySqlCommand normalPriceCommand;

        private readonly MySqlCommand latestPriceCommand;

        private readonly MySqlCommand updateProductPriceCommand;

        public ProductLineService(IConfiguration configuration)
        {
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            updateProductLineCommand =
                new MySqlCommand(
                    "INSERT INTO ProductLine (Id, Url, ImageUrl, Title, Category) "
                    + "VALUES (@Id, @Url, @ImageUrl, @Title, @Category) "
                    + "ON DUPLICATE KEY UPDATE Url=VALUES(Url), ImageUrl=VALUES(ImageUrl), Title=VALUES(Title), Category=VALUES(Category)",
                    connection);
            insertProductPriceCommand = new MySqlCommand(
                "INSERT INTO ProductPrice (Product, Price, PremiumPrice, Timestamp, DefaultPrice) VALUES (@Product, @Price, @PremiumPrice, @Timestamp, @DefaultPrice)",
                connection);
            normalPriceCommand =
                new MySqlCommand(
                    "SELECT Price, PremiumPrice, DefaultPrice from ProductPrice WHERE Product = @ProductId ORDER BY Timestamp DESC LIMIT 1",
                    connection);
            latestPriceCommand = new MySqlCommand("SELECT Price, PremiumPrice, Timestamp FROM ProductPrice WHERE Product = @ProductId ORDER BY Timestamp DESC LIMIT 1", connection);
            updateProductPriceCommand = new MySqlCommand("UPDATE ProductPrice SET Timestamp = NOW() WHERE Product = @ProductId AND Timestamp = @Timestamp LIMIT 1", connection);

            updateProductLineCommand.Prepare();
            insertProductPriceCommand.Prepare();
            normalPriceCommand.Prepare();
            updateProductPriceCommand.Prepare();
            latestPriceCommand.Prepare();
        }

        public void UpdateProductLine(ProductLine productLine)
        {
            Log.Debug("Updating product line information.");
            updateProductLineCommand.Parameters.Clear();
            updateProductLineCommand.Parameters.AddWithValue("@Id", productLine.Id);
            updateProductLineCommand.Parameters.AddWithValue("@Url", productLine.Url);
            updateProductLineCommand.Parameters.AddWithValue("@ImageUrl", productLine.ImageUrl);
            updateProductLineCommand.Parameters.AddWithValue("@Title", productLine.Title);
            updateProductLineCommand.Parameters.AddWithValue("@Category", productLine.Category);
            updateProductLineCommand.ExecuteNonQuery();
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
                InsertProductPrice(productLine);
            }
        }

        public void UpdateProductData(ProductLine productLine)
        {
            Log.Debug(string.Format("Updating product line {0}", productLine.Id));

            try
            {
                UpdateProductLine(productLine);
                UpdateLatestPrice(productLine);
            }
            catch (MySqlException ex)
            {
                Log.Error(string.Format("Error trying to product data {0}", productLine.Id), ex);

                throw;
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
            updateProductLineCommand.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }
}
