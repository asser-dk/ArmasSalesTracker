namespace Asser.ArmasSalesTracker.Services
{
    using System.Collections.Generic;
    using System.Linq;
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

        private readonly MySqlCommand addProductToFrontpageCommand;

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

            addProductToFrontpageCommand =
                new MySqlCommand(
                    "INSERT INTO Frontpage (Product, Title, ImageUrl, Category, Url, DefaultPrice, CurrentPrice, PremiumPrice) VALUES (@ProductId, @Title, @ImageUrl, @Category, @Url, @DefaultPrice, @CurrentPrice, @PremiumPrice)",
                    connection);

            updateProductCommand.Prepare();
            getProductsCommand.Prepare();
            addProductToFrontpageCommand.Prepare();
        }

        public void Dispose()
        {
            Log.Debug("Disposing product service");
            updateProductCommand.Dispose();
            getProductsCommand.Dispose();
            addProductToFrontpageCommand.Dispose();
            connection.Close();
            connection.Dispose();
        }

        public void UpdateProductData(Product productLine)
        {
            Log.Debug(string.Format("Updating product data for \"{0}\" (Id: {1})", productLine.Title, productLine.Id));
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
                while (reader.Read())
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

        public void ClearFrontpage()
        {
            Log.Debug("Clearing frontpage");
            using (var command = new MySqlCommand("TRUNCATE TABLE Frontpage", connection))
            {
                command.Prepare();
                command.ExecuteNonQuery();
            }
        }

        public void AddToFrontpage(Product product)
        {
            Log.Info("Adding " + product.Title + " (Id: " + product.Id + ") to the frontpage.");
            addProductToFrontpageCommand.Parameters.Clear();
            addProductToFrontpageCommand.Parameters.AddWithValue("@ProductId", product.Id);
            addProductToFrontpageCommand.Parameters.AddWithValue("@Title", product.Title);
            addProductToFrontpageCommand.Parameters.AddWithValue("@Category", product.Category);
            addProductToFrontpageCommand.Parameters.AddWithValue("@ImageUrl", product.ImageUrl);
            addProductToFrontpageCommand.Parameters.AddWithValue("@Url", product.Url);
            addProductToFrontpageCommand.Parameters.AddWithValue("@DefaultPrice", product.PriceInfo.First(x => x.Type == PriceTypes.Default).Value);
            addProductToFrontpageCommand.Parameters.AddWithValue("@CurrentPrice", product.PriceInfo.First(x => x.Type == PriceTypes.Current).Value);
            var premiumPrice = product.PriceInfo.Any(x => x.Type == PriceTypes.CurrentPremium)
                                   ? product.PriceInfo.First(x => x.Type == PriceTypes.CurrentPremium).Value
                                   : 0;
            addProductToFrontpageCommand.Parameters.AddWithValue("@PremiumPrice", premiumPrice);
            addProductToFrontpageCommand.ExecuteNonQuery();
        }
    }
}
