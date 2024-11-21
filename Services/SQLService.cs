using System;
using System.Data;
using System.Reflection;
using CrimsonBanned.Structs;
using MySql.Data.MySqlClient;

namespace CrimsonBanned.Services;
internal class SQLService
{
    private static string connectionString;

    public SQLService()
    {
        var assembly = Assembly.GetExecutingAssembly();
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string resourceName = args.Name switch
            {
                string name when name.StartsWith("MySql.Data") => "CrimsonBanned._9._1._0.lib.net6._0.MySql.Data.dll",
                string name when name.StartsWith("System.Diagnostics.DiagnosticSource") => "CrimsonBanned._8._0._1.lib.net6._0.System.Diagnostics.DiagnosticSource.dll",
                string name when name.StartsWith("System.Security.Permissions") => "CrimsonBanned._8._0._0.lib.net6._0.System.Security.Permissions.dll",
                string name when name.StartsWith("System.Configuration.ConfigurationManager") => "CrimsonBanned._8._0._0.lib.net6._0.System.Configuration.ConfigurationManager.dll",
                string name when name.StartsWith("System.Text.Encoding.CodePages") => "CrimsonBanned._8._0._0.lib.net6._0.System.Text.Encoding.CodePages.dll",
                _ => null
            };

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    var assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            }
            return null;
        };

        connectionString = $"Server={Settings.Host.Value};Database={Settings.MySQLDbName.Value};User ID={Settings.UserName.Value};Password={Settings.Password.Value};";
    }

    public void Connect()
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            // Connection successful
        }
    }

    public void InsertBan(string tableName, string playerName, ulong playerID, string reason, DateTime timeUntil)
    {
        if(!Settings.MySQLConfigured) return;

        string query = $@"
        INSERT INTO {tableName} (PlayerName, PlayerID, Reason, TimeUntil) 
        VALUES (@PlayerName, @PlayerID, @Reason, @TimeUntil);";

        using (var connection = new MySqlConnection(connectionString))
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PlayerName", playerName);
            command.Parameters.AddWithValue("@PlayerID", playerID);
            command.Parameters.AddWithValue("@Reason", reason);
            command.Parameters.AddWithValue("@TimeUntil", timeUntil);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public DataTable GetBans(string tableName)
    {
        string query = $"SELECT * FROM {tableName};";

        using (var connection = new MySqlConnection(connectionString))
        using (var command = new MySqlCommand(query, connection))
        {
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                DataTable results = new DataTable();
                results.Load(reader);
                return results;
            }
        }
    }

    public void DeleteBan(string tableName, ulong playerId)
    {
        string query = $@"
        DELETE FROM {tableName}
        WHERE PlayerID = @PlayerID;";

        using (var connection = new MySqlConnection(connectionString))
        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PlayerID", playerId);

            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void ExecuteQuery(string query)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            using (var command = new MySqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public MySqlDataReader ExecuteReader(string query)
    {
        var connection = new MySqlConnection(connectionString);
        connection.Open();
        var command = new MySqlCommand(query, connection);
        return command.ExecuteReader(CommandBehavior.CloseConnection);
    }

    public void InitializeTables()
    {
        string[] tableCreationQueries = {
        @"
        CREATE TABLE IF NOT EXISTS Banned (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            PlayerName VARCHAR(255) NOT NULL,
            PlayerID BIGINT UNSIGNED NOT NULL,
            TimeUntil DATETIME NOT NULL,
            Reason TEXT
        );",
        @"
        CREATE TABLE IF NOT EXISTS Chat (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            PlayerName VARCHAR(255) NOT NULL,
            PlayerID BIGINT UNSIGNED NOT NULL,
            TimeUntil DATETIME NOT NULL,
            Reason TEXT
        );",
        @"
        CREATE TABLE IF NOT EXISTS Voice (
            Id INT AUTO_INCREMENT PRIMARY KEY,
            PlayerName VARCHAR(255) NOT NULL,
            PlayerID BIGINT UNSIGNED NOT NULL,
            TimeUntil DATETIME NOT NULL,
            Reason TEXT
        );"
        };

        foreach (var query in tableCreationQueries)
        {
            ExecuteQuery(query);
        }
    }
}
