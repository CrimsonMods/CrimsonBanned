using System.Collections.Generic;

namespace CrimsonBanned.Structs;

public static class SQLlink
{
    public static void InitializeBanTables()
    {
        var columns = new Dictionary<string, string>
        {
            ["Id"] = "INT AUTO_INCREMENT PRIMARY KEY",
            ["PlayerName"] = "VARCHAR(255) NOT NULL",
            ["PlayerID"] = "BIGINT UNSIGNED NOT NULL",
            ["TimeUntil"] = "DATETIME NOT NULL",
            ["Reason"] = "TEXT",
            ["Issued"] = "DATETIME NOT NULL",
            ["IssuedBy"] = "VARCHAR(255) NOT NULL"
        };

        Database.SQL.CreateTable("Banned", columns);
        Database.SQL.CreateTable("Chat", columns);
        Database.SQL.CreateTable("Voice", columns);
    }

    public static void AddBan(Ban ban, List<Ban> list)
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

        string tableName = list == Database.ChatBans ? "Chat" :
                          list == Database.VoiceBans ? "Voice" : "Banned";

        int i = Database.SQL.Insert(tableName, values);
        if(i >= 0) ban.DatabaseId = i;
    }

    public static void DeleteBan(Ban ban, List<Ban> list)
    {
        var whereConditions = new Dictionary<string, object>
        {
            ["Id"] = ban.DatabaseId
        };

        string tableName = list == Database.ChatBans ? "Chat" :
                          list == Database.VoiceBans ? "Voice" : "Banned";

        Database.SQL.Delete(tableName, whereConditions);
    }
}