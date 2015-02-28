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

            updateProductCommand.Prepare();
        }

        public void Dispose()
        {
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
    }
}
