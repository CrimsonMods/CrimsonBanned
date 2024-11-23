using CrimsonBanned.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace CrimsonBanned.Structs;

internal class Database
{
    public static readonly JsonSerializerOptions prettyJsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };

    public static string BannedFile = Path.Combine(Plugin.ConfigFiles, "banned.json");
    public static string ChatBanFile = Path.Combine(Plugin.ConfigFiles, "bans_chat.json");
    public static string VoiceBanFile = Path.Combine(Plugin.ConfigFiles, "bans_voice.json");

    public static List<Ban> Banned;
    public static List<Ban> ChatBans;
    public static List<Ban> VoiceBans;

    public static SQLService SQL;

    public Database()
    {
        LoadDatabases();
    }

    private static void LoadDatabases()
    {
        if (!Directory.Exists(Plugin.ConfigFiles)) { Directory.CreateDirectory(Plugin.ConfigFiles); }

        if (File.Exists(ChatBanFile))
        {
            string json = File.ReadAllText(ChatBanFile);
            ChatBans = JsonSerializer.Deserialize<List<Ban>>(json, prettyJsonOptions);
        }
        else
        {
            ChatBans = new List<Ban>();
        }

        if (File.Exists(VoiceBanFile))
        {
            string json = File.ReadAllText(VoiceBanFile);
            VoiceBans = JsonSerializer.Deserialize<List<Ban>>(json, prettyJsonOptions);
        }
        else
        {
            VoiceBans = new List<Ban>();
        }

        if (File.Exists(BannedFile))
        {
            string json = File.ReadAllText(BannedFile);
            Banned = JsonSerializer.Deserialize<List<Ban>>(json, prettyJsonOptions);
        }
        else
        {
            Banned = new List<Ban>();
        }

        StartSQLConnection();
    }

    private static async void StartSQLConnection()
    {
        if (!Settings.MySQLConfigured) return;

        SQL = new();
        SQL.Connect();
        SQL.InitializeTables();
        await Task.Yield();
        SyncDB();

        Core.StartCoroutine(SyncLoop());
    }

    public static void AddBan(Ban ban, List<Ban> list)
    {
        if (list == ChatBans)
        {
            ChatBans.Add(ban);
            SQL.InsertBan("Chat", ban.PlayerName, ban.PlayerID, ban.Reason, ban.TimeUntil);
        }
        else if (list == VoiceBans)
        {
            VoiceBans.Add(ban);
            SQL.InsertBan("Voice", ban.PlayerName, ban.PlayerID, ban.Reason, ban.TimeUntil);
        }
        else
        {
            Banned.Add(ban);
            SQL.InsertBan("Banned", ban.PlayerName, ban.PlayerID, ban.Reason, ban.TimeUntil);
        }

        SaveDatabases();
    }

    public static void DeleteBan(Ban ban, List<Ban> list)
    {
        if (list == ChatBans)
        {
            ChatBans.Remove(ban);
            SQL.DeleteBan("Chat", ban.PlayerID);
        }
        else if (list == VoiceBans)
        {
            VoiceBans.Remove(ban);
            SQL.DeleteBan("Voice", ban.PlayerID);
        }
        else
        {
            Banned.Remove(ban);
            SQL.DeleteBan("Banned", ban.PlayerID);
        }

        SaveDatabases();
    }

    private static void SaveDatabases()
    {
        if (ChatBans.Count > 0)
        {
            string json = JsonSerializer.Serialize(ChatBans, prettyJsonOptions);
            File.WriteAllText(ChatBanFile, json);
        }

        if (VoiceBans.Count > 0)
        {
            string json = JsonSerializer.Serialize(VoiceBans, prettyJsonOptions);
            File.WriteAllText(VoiceBanFile, json);
        }

        if (Banned.Count > 0)
        {
            string json = JsonSerializer.Serialize(Banned, prettyJsonOptions);
            File.WriteAllText(BannedFile, json);
        }
    }

    public static void SyncDB()
    {
        SyncTable(ChatBans, "Chat");
        SyncTable(VoiceBans, "Voice");
        SyncTable(Banned, "Banned");
    }

    private static void SyncTable(List<Ban> list, string tableName)
    {
        DataTable table = SQL.GetBans(tableName);
        foreach (DataRow row in table.Rows)
        {
            Ban ban = new Ban(
                row["PlayerName"].ToString(),
                Convert.ToUInt64(row["PlayerID"]),
                Convert.ToDateTime(row["TimeUntil"]),
                row["Reason"].ToString()
            );

            if (!list.Contains(ban))
            {
                list.Add(ban);
            }
        }
    }

    static IEnumerator SyncLoop()
    {
        while (true)
        {
            foreach (Ban ban in Banned)
            {
                if (ban.TimeUntil == DateTime.MinValue) continue;

                if (ban.TimeUntil < DateTime.Now)
                {
                    DeleteBan(ban, Banned);
                    if (File.Exists(Settings.BanFilePath.Value))
                    {
                        var lines = File.ReadAllLines(Settings.BanFilePath.Value).ToList();
                        lines.RemoveAll(line => line.Trim() == ban.PlayerID.ToString());
                        File.WriteAllLines(Settings.BanFilePath.Value, lines);
                    }
                }
            }

            yield return new WaitForSeconds(Settings.SyncInterval.Value * 60 + UnityEngine.Random.Range(-10, 20));
            SyncDB();
        }
    }
}