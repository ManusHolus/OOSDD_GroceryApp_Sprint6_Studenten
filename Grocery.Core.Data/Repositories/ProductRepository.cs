using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    public class ProductRepository : DatabaseConnection, IProductRepository
    {
        private readonly List<Product> products;

        public ProductRepository()
        {
            // make a product list
            CreateTable(@"CREATE TABLE IF NOT EXISTS ProductList (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [Name] NVARCHAR(80) NOT NULL,
                            [Stock] INTEGER NOT NULL,
                            [ShelfLife] DATE NOT NULL,
                            [Price] DECIMAL(10,2) NOT NULL
                        );");

            // already set up list we got
            products = [
                new Product(1, "Melk", 300, new DateOnly(2025, 9, 25), 0.95m),
                new Product(2, "Kaas", 100, new DateOnly(2025, 9, 30), 7.98m),
                new Product(3, "Brood", 400, new DateOnly(2025, 9, 12), 2.19m),
                new Product(4, "Cornflakes", 0, new DateOnly(2025, 12, 31), 1.48m)
            ];
        }

        public List<Product> GetAll()
        {
            products.Clear();
            string selectQuery = "SELECT Id, Name, Stock, ShelfLife, Price FROM ProductList";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    int stock = reader.GetInt32(2);
                    DateOnly shelfLife = DateOnly.FromDateTime(reader.GetDateTime(3));
                    decimal price = reader.GetDecimal(4);

                    products.Add(new(id, name, stock, shelfLife, price));
                }
            }
            CloseConnection();
            return products;
        }

        public Product? Get(int id)
        {
            return products.FirstOrDefault(p => p.Id == id);
        }

        public Product Add(Product item)
        {
            string insertQuery = @"
                INSERT INTO ProductList(Name, Stock, ShelfLife, Price)
                VALUES(@Name, @Stock, @ShelfLife, @Price);
                SELECT last_insert_rowid();";

            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Stock", item.Stock);
                command.Parameters.AddWithValue("@ShelfLife", item.ShelfLife);
                command.Parameters.AddWithValue("@Price", item.Price);

                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            CloseConnection();

            products.Add(item); 
            return item;
        }

        public Product? Delete(Product item)
        {
            string deleteQuery = $"DELETE FROM ProductList WHERE Id = {item.Id};";
            OpenConnection();
            using (SqliteCommand command = new(deleteQuery, Connection))
            {
                command.ExecuteNonQuery();
            }
            CloseConnection();

            products.RemoveAll(p => p.Id == item.Id);
            return item;
        }

        public Product? Update(Product item)
        {
            string updateQuery = @"UPDATE ProductList 
                                   SET Name = @Name, Stock = @Stock, ShelfLife = @ShelfLife, Price = @Price
                                   WHERE Id = @Id;";

            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Stock", item.Stock);
                command.Parameters.AddWithValue("@ShelfLife", item.ShelfLife);
                command.Parameters.AddWithValue("@Price", item.Price);
                command.Parameters.AddWithValue("@Id", item.Id);

                command.ExecuteNonQuery();
            }
            CloseConnection();

            // once you're out of the connection it updates the existing items
            var existing = products.FirstOrDefault(p => p.Id == item.Id);
            if (existing != null)
            {
                existing.Name = item.Name;
                existing.Stock = item.Stock;
                existing.ShelfLife = item.ShelfLife;
                existing.Price = item.Price;
            }

            return item;
        }
    }
}
