namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Reflection;
    using Asser.ArmasSalesTracker.Configuration;
    using Asser.ArmasSalesTracker.Models;
    using log4net;
    using MySql.Data.MySqlClient;

    public class ProductService : IProductService
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly MySqlConnection connection;

        private readonly MySqlCommand updateProductCommand;

        private readonly MySqlCommand getProductsCommand;

        public ProductService(IConfiguration configuration)
        {
            connection = new MySqlConnection(configuration.MySqlConnectionString);
            connection.Open();

            updateProductCommand =
                new MySqlCommand(
                    "INSERT INTO Product (Id, Url, ImageUrl, Title, Category) "
                    + "VALUES (@Id, @Url, @ImageUrl, @Title, @Category) "
                    + "ON DUPLICATE KEY UPDATE Url=VALUES(Url), ImageUrl=VALUES(ImageUrl), Title=VALUES(Title), Category=VALUES(Category)",
                    connection);

            getProductsCommand = new MySqlCommand("SELECT Id, Url, ImageUrl, Title, Category FROM Product", connection);

            updateProductCommand.Prepare();
            getProductsCommand.Prepare();
        }

        public void Dispose()
        {
            Log.Debug("Disposing product service");
            updateProductCommand.Dispose();
            getProductsCommand.Dispose();
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

        public IEnumerable<Product> GetProducts()
        {
            Log.Info("Getting all products");
            using (var reader = getProductsCommand.ExecuteReader())
            {
                yield return
                    new Product
                    {
                        Id = reader.GetString("Id"),
                        Category = reader.GetString("Category"),
                        ImageUrl = reader.GetString("ImageUrl"),
                        Title = reader.GetString("Title"),
                        Url = reader.GetString("Url")
                    };
            }
        }
    }
}
