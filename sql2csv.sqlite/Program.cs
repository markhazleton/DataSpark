// See https://aka.ms/new-console-template for more information
using Microsoft.Data.Sqlite;

Console.WriteLine("Hello, World!");
string connectionString = "Data Source=mydatabase.db";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    // Create a table
    var createTableCmd = connection.CreateCommand();
    createTableCmd.CommandText =
    @"
                    CREATE TABLE IF NOT EXISTS People (
                        Id INTEGER PRIMARY KEY,
                        Name TEXT NOT NULL
                    )
                ";
    createTableCmd.ExecuteNonQuery();

    // Insert some rows
    var insertCmd = connection.CreateCommand();
    insertCmd.CommandText =
    @"
                    INSERT INTO People (Name) VALUES ('John Doe');
                    INSERT INTO People (Name) VALUES ('Jane Doe');
                ";
    insertCmd.ExecuteNonQuery();

    // Select and display rows
    var selectCmd = connection.CreateCommand();
    selectCmd.CommandText =
    @"
                    SELECT Id, Name FROM People
                ";

    using (var reader = selectCmd.ExecuteReader())
    {
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            Console.WriteLine($"ID: {id}, Name: {name}");
        }
    }
}