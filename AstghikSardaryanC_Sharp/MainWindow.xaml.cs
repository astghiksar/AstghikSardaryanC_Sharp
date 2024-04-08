using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Xps.Serialization;
using System.Xml.Linq;

namespace ProductManagement
{
    public partial class MainWindow : Window
    {
        // A collection to store product data
        private ObservableCollection<Product> products = new ObservableCollection<Product>();

        public MainWindow()
        {
            InitializeComponent();
            //  Display the collection of items with ListView
            productListView.ItemsSource = products;
            // Populate the "products" collection with the data from the database
            LoadProducts();
        }

        // An error handling method(checks if the barcode is a numeric value and has exactly 12 digits)
        private bool ValidateBarcode(string barcode)
        {
            if (barcode.Length != 12 || !barcode.All(char.IsDigit))
            {
                MessageBox.Show("Invalid barcode. Please enter a 12-digit numeric value for barcode.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {

            // Checks if a product is selected in the list view and if the selected item is a Product object

            Product[] selectedProductsCopy = new Product[] { };
            selectedProductsCopy = productListView.SelectedItems.Cast<Product>().ToArray();

            foreach (Product product in selectedProductsCopy)
            {
                if (product != null)
                {
                    DeleteProduct(product);
                }
                else
                {
                    MessageBox.Show("Please select a product to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteProduct(Product productToDelete)
        {
            // Remove the product from the ObservableCollection
            products.Remove(productToDelete);

            // Delete the product from the database
            string connectionString = @"Data Source=.\;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM Products WHERE Barcode = @Barcode";

                SqlCommand command = new SqlCommand(query, connection);
                // Specifying the value of the barcode that should be deleted
                command.Parameters.AddWithValue("@Barcode", productToDelete.Barcode);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the text(the barcode) entered by the user in the text box
            string barcodeToSearch = txtSearchBarcode.Text;


            if (!ValidateBarcode(barcodeToSearch))
            {

            }
            // This condition checks if the barcode is null or empty or if if it's not valid
            if (string.IsNullOrEmpty(barcodeToSearch) || !ValidateBarcode(barcodeToSearch))
            {
                MessageBox.Show("Please enter a valid barcode to search.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Displaying the search results
            Product foundProduct = SearchProductByBarcode(barcodeToSearch);
            if (foundProduct != null)
            {
                MessageBox.Show($"Product found:\nBarcode: {foundProduct.Barcode}\nName: {foundProduct.Name}\nSupplier: {foundProduct.Supplier}\nPrice: {foundProduct.Price}\nQuantity: {foundProduct.Quantity}", "Product Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Product not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Product SearchProductByBarcode(string barcode)
        {
            string connectionString = @"Data Source=.\;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Looks for a row that matched the barcode the user has typed in the database
                string query = "SELECT * FROM Products WHERE Barcode = @Barcode";

                SqlCommand command = new SqlCommand(query, connection);
                // Passing the value to the database
                command.Parameters.AddWithValue("@Barcode", barcode);

                connection.Open();

                // Read the data from the database
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    // This condition checks if there are any rows returned by the query
                    // if the row is would it extract the product details from the reader
                    // and creates a new "Product" object with the extracted data.
                    if (reader.Read())
                    {
                        string foundBarcode = reader["Barcode"].ToString();
                        string name = reader["Name"].ToString();
                        string supplier = reader["Supplier"].ToString();
                        decimal price = (decimal)reader["Price"];
                        int quantity = (int)reader["Quantity"];

                        return new Product(foundBarcode, name, supplier, price, quantity);
                    }
                }
            }
            return null;
        }


        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            string barcode = txtBarcode.Text;
            string name = txtName.Text;
            string supplier = txtSupplier.Text;
            int quantity;
            decimal price;

            // Check if the barcode input is valid
            if (barcode.Length != 12 || !barcode.All(char.IsDigit))
            {
                MessageBox.Show("Invalid barcode. Please enter a 12-digit numeric value for barcode.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if the quantity input is valid
            if (!int.TryParse(txtQuantity.Text, out quantity))
            {
                MessageBox.Show("Invalid quantity. Please enter a valid numeric value for quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check if the price input is valid
            if (!decimal.TryParse(txtPrice.Text, out price))
            {
                MessageBox.Show("Invalid price. Please enter a valid numeric value for price.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure the price is not negative
            if (price < 0)
            {
                MessageBox.Show("Price cannot be negative. Please enter a valid price value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // check for an existing product to prevent duplicate entries
            // if such product exists it will be assigned to the existingProduct variable, if not it will return null
            Product existingProduct = products.FirstOrDefault(p =>
                p.Barcode == barcode &&
                p.Name == name &&
                p.Supplier == supplier &&
                p.Price == price);

            if (existingProduct != null)
            {
                // Update the quantity of the existing product
                existingProduct.Quantity += quantity;
                UpdateProductQuantityInDatabase(existingProduct);
            }
            else
            {
                // Add the new product to the collection
                Product newProduct = new Product(barcode, name, supplier, price, quantity);
                products.Add(newProduct);
                SaveProduct(newProduct);
            }

            ClearFields();
        }



        // A method for TextBox activation
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // if it was indeed textbox that was activated and the text in it started with "Enter"
            if (sender is TextBox textBox && textBox.Text.StartsWith("Enter"))
            {
                // clear the text in the textbox
                textBox.Text = string.Empty;
            }
        }




        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Checks if the sender is indeed a textbox and the text content is empty or whitespace
            if (sender is TextBox textBox && string.IsNullOrWhiteSpace(textBox.Text))
            {
                // Sets placeholder texts for each of the textboxes
                if (textBox.Name == "txtBarcode")
                    textBox.Text = "Enter Barcode";
                else if (textBox.Name == "txtName")
                    textBox.Text = "Enter Name";
                else if (textBox.Name == "txtSupplier")
                    textBox.Text = "Enter Supplier";
                else if (textBox.Name == "txtPrice")
                    textBox.Text = "Enter Price";
                else if (textBox.Name == "txtQuantity")
                    textBox.Text = "Enter Quantity";
            }
        }

        // A method to clear the field after adding the products
        private void ClearFields()
        {
            txtBarcode.Clear();
            txtName.Clear();
            txtSupplier.Clear();
            txtPrice.Clear();
            txtQuantity.Clear();
        }

        private void SaveProduct(Product product)
        {
            string connectionString = @"Data Source=.\;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Insert the values to the Products table
                string query = "INSERT INTO Products (Barcode, Name, Supplier, Price, Quantity) VALUES (@Barcode, @Name, @Supplier, @Price, @Quantity)";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Barcode", product.Barcode);
                command.Parameters.AddWithValue("@Name", product.Name);
                command.Parameters.AddWithValue("@Supplier", product.Supplier);
                command.Parameters.AddWithValue("@Price", product.Price);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        // A method that fetches data from the database and populates the collection with it
        private void LoadProducts()
        {
            string connectionString = @"Data Source=.\;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Products";

                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Checks if the retrieved data is not null and converts them into an appropriate type
                        string barcode = reader["Barcode"] != DBNull.Value ? reader["Barcode"].ToString() : string.Empty;
                        string name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : string.Empty;
                        string supplier = reader["Supplier"] != DBNull.Value ? reader["Supplier"].ToString() : string.Empty;
                        decimal price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0;
                        int quantity = reader["Quantity"] != DBNull.Value ? Convert.ToInt32(reader["Quantity"]) : 0;
                        // Creates a Product object
                        Product product = new Product(barcode, name, supplier, price, quantity);
                        // And populates the collection with it
                        products.Add(product);
                    }
                }
            }
        }

        private void UpdateProductQuantityInDatabase(Product product)
        {
            string connectionString = @"Data Source=.\;Initial Catalog=InventoryDB;Integrated Security=True;TrustServerCertificate=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // Updates the corresponding quantity in the table
                string query = "UPDATE Products SET Quantity = @Quantity WHERE Barcode = @Barcode";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Barcode", product.Barcode);
                command.Parameters.AddWithValue("@Quantity", product.Quantity);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }

    public class Product
    {
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Supplier { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public Product(string barcode, string name, string supplier, decimal price, int quantity)
        {
            Barcode = barcode;
            Name = name;
            Supplier = supplier;
            Price = price;
            Quantity = quantity;
        }
    }
}


