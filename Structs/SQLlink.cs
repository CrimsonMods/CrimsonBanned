using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.Collections;

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
            ["TimeUntil"] = "DATETIME NOT NULL DEFAULT UTC_TIMESTAMP()",
            ["Reason"] = "TEXT",
            ["Issued"] = "DATETIME NOT NULL DEFAULT UTC_TIMESTAMP()",
            ["IssuedBy"] = "VARCHAR(255)"
        };
        Database.SQL.CreateTable("Banned", columns);
        Database.SQL.CreateTable("Chat", columns);
        Database.SQL.CreateTable("Voice", columns);
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

        string tableName = list == Database.ChatBans ? "Chat" :
                          list == Database.VoiceBans ? "Voice" : "Banned";

        List<int> errors = new List<int>() {
            -1062,
            1062
        };

        int i = Database.SQL.Insert(tableName, values, errors);
        return i;
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

    public static Ban GetBan(ulong playerID, List<Ban> list)
    {
        var whereConditions = new Dictionary<string, object>
        {
            ["PlayerID"] = playerID
        };
        string tableName = list == Database.ChatBans ? "Chat" :
                            list == Database.VoiceBans ? "Voice" : "Banned";

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
        return Database.SQL.Connect();
    }

    public static int ResolveConflict(Ban ban, List<Ban> list, bool removeFromSQL = false)
    {
        Ban banFromDB = GetBan(ban.PlayerID, list);

        if (banFromDB != null)
        {
            if(banFromDB.TimeUntil == DateTime.MinValue && ban.TimeUntil == DateTime.MinValue)
            {
                Database.AddBan(banFromDB, list);
                return 0;
            }

            if (ban.TimeUntil < banFromDB.TimeUntil || banFromDB.TimeUntil == DateTime.MinValue)
            {
                if (removeFromSQL)
                {
                    Database.DeleteBan(ban, list, true);
                }

                Database.AddBan(banFromDB, list);
                return 1;
            }
            else if (ban.TimeUntil > banFromDB.TimeUntil)
            {
                DeleteBan(banFromDB, list);
                int i = AddBan(ban, list);
                ban.DatabaseId = i;
                Database.AddBan(ban, list);
                return 2;
            }

            return 3;
        }
        else
            return -1000;
    }

    public static void ResolveOfflines()
    {
        List<Ban> Chat = Database.ChatBans.FindAll(x => x.DatabaseId == -1);
        List<Ban> Voice = Database.VoiceBans.FindAll(x => x.DatabaseId == -1);
        List<Ban> Banned = Database.Banned.FindAll(x => x.DatabaseId == -1);

        foreach (Ban ban in Chat)
        {
            int i = AddBan(ban, Database.ChatBans);
            if (i >= 0)
            {
                ban.DatabaseId = i;
            }
            else
                ResolveConflict(ban, Database.ChatBans);
        }

        foreach (Ban ban in Voice)
        {
            int i = AddBan(ban, Database.VoiceBans);
            if (i >= 0)
            {
                ban.DatabaseId = i;
            }
            else
                ResolveConflict(ban, Database.VoiceBans);
        }

        foreach (Ban ban in Banned)
        {
            int i = AddBan(ban, Database.Banned);
            if (i >= 0)
            {
                ban.DatabaseId = i;
            }
            else
                ResolveConflict(ban, Database.Banned);
        }
    }
}