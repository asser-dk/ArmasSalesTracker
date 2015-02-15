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

        private readonly MySqlCommand updateProductLineCommand;

        private readonly MySqlCommand insertProductPriceCommand;

        private readonly MySqlCommand normalPriceCommand;

        public ProductLineService(IConfiguration configuration)
        {
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            updateProductLineCommand = new MySqlCommand(
                "INSERT INTO ProductLine (Id, Url, ImageUrl, Title) VALUES (@Id, @Url, @ImageUrl, @Title) "
                + "ON DUPLICATE KEY UPDATE Url=VALUES(Url), ImageUrl=VALUES(ImageUrl), Title=VALUES(Title)",
                connection);
            insertProductPriceCommand = new MySqlCommand(
                "INSERT INTO ProductPrice (Product, Price, PremiumPrice) VALUES (@Product, @Price, @PremiumPrice)",
                connection);
            normalPriceCommand =
                new MySqlCommand(
                    "SELECT Price, PremiumPrice from ProductPrice WHERE Product = @ProductId GROUP BY Price, PremiumPrice ORDER BY COUNT(*) DESC",
                    connection);

            updateProductLineCommand.Prepare();
            insertProductPriceCommand.Prepare();
            normalPriceCommand.Prepare();
        }

        public void UpdateProductLine(ProductLine productLine)
        {
            Log.Debug(string.Format("Updating product line {0}", productLine.Id));

            try
            {
                updateProductLineCommand.Parameters.Clear();
                updateProductLineCommand.Parameters.AddWithValue("@Id", productLine.Id);
                updateProductLineCommand.Parameters.AddWithValue("@Url", productLine.Url);
                updateProductLineCommand.Parameters.AddWithValue("@ImageUrl", productLine.ImageUrl);
                updateProductLineCommand.Parameters.AddWithValue("@Title", productLine.Title);
                updateProductLineCommand.ExecuteNonQuery();

                insertProductPriceCommand.Parameters.Clear();
                insertProductPriceCommand.Parameters.AddWithValue("@Product", productLine.Id);
                insertProductPriceCommand.Parameters.AddWithValue("@Price", productLine.Price);
                insertProductPriceCommand.Parameters.AddWithValue("@PremiumPrice", productLine.PremiumPrice);
                insertProductPriceCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Log.Error("Error trying to update product line " + productLine.Id, ex);

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
                    Premium = results.GetInt32("PremiumPrice")
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
