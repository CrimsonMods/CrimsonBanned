using System;
using System.Collections.Generic;
using CrimsonBanned.Utilities;

namespace CrimsonBanned.Structs;

public static class SQLlink
{
    public static void InitializeBanTables()
    {
        var columns = new Dictionary<string, string>
        {
            ["Id"] = "INT AUTO_INCREMENT PRIMARY KEY",
            ["PlayerName"] = "VARCHAR(255)",
            ["PlayerID"] = "BIGINT UNSIGNED NOT NULL UNIQUE",
            ["TimeUntil"] = "DATETIME NOT NULL",
            ["Reason"] = "TEXT",
            ["Issued"] = "DATETIME NOT NULL",
            ["IssuedBy"] = "VARCHAR(255)"
        };
        Database.SQL.CreateTable("ServerBans", columns);
        Database.SQL.CreateTable("ChatBans", columns);
        Database.SQL.CreateTable("VoiceBans", columns);
    }

    public static int AddBan(Ban ban, List<Ban> list)
    {
        var values = new Dictionary<string, object>
        {
            ["PlayerName"] = ban.PlayerName,
            ["PlayerID"] = ban.PlayerID,
            ["TimeUntil"] = ban.TimeUntil,
            ["Reason"] = ban.Reason,
            ["Issued"] = ban.Issued,
            ["IssuedBy"] = ban.IssuedBy
        };

        string tableName = list == Database.ChatBans ? "ChatBans" :
                          list == Database.VoiceBans ? "VoiceBans" : "ServerBans";

        List<int> errors = new List<int>() {
            -1062, -1042
        };

        int i = Database.SQL.Insert(tableName, values, errors);

        if(i == -1042) i = -1;
        return i;
    }

    public static void DeleteBan(Ban ban, List<Ban> list)
    {
        var whereConditions = new Dictionary<string, object>
        {
            ["Id"] = ban.DatabaseId
        };

        string tableName = list == Database.ChatBans ? "ChatBans" :
                          list == Database.VoiceBans ? "VoiceBans" : "ServerBans";

        Database.SQL.Delete(tableName, whereConditions);
    }

    public static void DeleteBan(int id, string tableName)
    {
        var whereConditions = new Dictionary<string, object>
        {
            ["Id"] = id
        };

        Database.SQL.Delete(tableName, whereConditions);
    }

    public static Ban GetBan(ulong playerID, List<Ban> list)
    {
        var whereConditions = new Dictionary<string, object>
        {
            ["PlayerID"] = playerID
        };
        string tableName = list == Database.ChatBans ? "ChatBans" :
                            list == Database.VoiceBans ? "VoiceBans" : "ServerBans";

        var ban = Database.SQL.Select(tableName, new[] { "*" }, whereConditions);

        if (ban != null && ban.Rows.Count > 0)
        {
            var row = ban.Rows[0];
            Ban b = new Ban(
                row["PlayerName"].ToString(),
                Convert.ToUInt64(row["PlayerID"]),
                Convert.ToDateTime(row["TimeUntil"]),
                row["Reason"].ToString(),
                row["IssuedBy"].ToString()
            );

            b.DatabaseId = Convert.ToInt32(row["Id"]);
            b.Issued = Convert.ToDateTime(row["Issued"]);
            return b;
        }

        return null;
    }
    
    public static bool Connect()
    {
        if(Database.SQL == null) return false;
        return Database.SQL.Connect();
    }
}