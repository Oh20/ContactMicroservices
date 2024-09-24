using System.Data.SqlClient;

namespace DatabaseConnectionTests
{
    [TestFixture]
    public class DatabaseConnectionTests
    {
        [Test]
        [Category("DatabaseIntegration")]
        public async Task TestDatabaseConnection()
        {
            var connectionString = "Server=tcp:labserversqltreo.database.windows.net,1433;Initial Catalog=lab01;Persist Security Info=False;User ID=azureadmin;Password=Senha@2024;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            //var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Connection string is not set.");
                throw new InvalidOperationException("Database connection string is not set in the environment variables.");
            }

            Console.WriteLine("Connection string: " + connectionString);

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                Assert.AreEqual(System.Data.ConnectionState.Open, connection.State, "Failed to open connection to the database.");
            }
        }
    }
}