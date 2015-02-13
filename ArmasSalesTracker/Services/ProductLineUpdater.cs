namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;

    public class ProductLineUpdater : IProductLineUpdater
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConfiguration configuration;

        public ProductLineUpdater(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void UpdateProductLines(IEnumerable<ProductLine> productLines)
        {
            Log.Info("Updating database.");
            MySqlConnection connection = null;

            try
            {
                connection = new MySqlConnection(configuration.MySqlConnectionString);
                connection.Open();

                var updateProductLineCommand = new MySqlCommand(
                    "INSERT INTO ProductLine (Id, Url, ImageUrl, Title) VALUES (@Id, @Url, @ImageUrl, @Title) "
                    + "ON DUPLICATE KEY UPDATE Url=VALUES(Url), ImageUrl=VALUES(ImageUrl), Title=VALUES(Title)",
                    connection);
                var insertProductPriceCommand = new MySqlCommand(
                    "INSERT INTO ProductPrice (Product, Price, PremiumPrice) VALUES (@Product, @Price, @PremiumPrice)",
                    connection);

                updateProductLineCommand.Prepare();

                foreach (var productLine in productLines)
                {
                    Log.Debug(string.Format("Updating product id {0}", productLine.Id));
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
            }
            catch (MySqlException ex)
            {
                Log.Error("Error trying to update product lines", ex);

                throw;
            }
            finally
            {
                if (connection != null)
                {
                    connection.Close();
                }
            }
        }
    }
}
