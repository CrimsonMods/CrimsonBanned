using CrimsonBanned.Commands;
using CrimsonSQL.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using static CrimsonBanned.Services.PlayerService;
using BepInEx.Unity.IL2CPP;

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
    public static string MessageFile = Path.Combine(Plugin.ConfigFiles, "messages.json");

    public static List<Ban> Banned;
    public static List<Ban> ChatBans;
    public static List<Ban> VoiceBans;
    public static List<MessagePair> Messages;

    public static ISQLService SQL => IL2CPPChainloader.Instance.Plugins.TryGetValue("CrimsonSQL", out var pluginInfo)
    ? CrimsonSQL.Plugin.SQLService
    : null;

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

        if(File.Exists(MessageFile))
        {
            string json = File.ReadAllText(MessageFile);
            Messages = JsonSerializer.Deserialize<List<MessagePair>>(json, prettyJsonOptions);
        }
        else
        {
            Messages =
            [
                new MessagePair("CheckHeader", "\n{player}'s ({id}) Bans:"),
                new MessagePair("CheckBanLine", "\n{type} Ban\nIssued: {issued}\nRemaining: {remaining}\nReason: {reason}"),
                new MessagePair("ListBan", "\n{player} ({id}) - {remaining}")
            ];

            string json = JsonSerializer.Serialize(Messages, prettyJsonOptions);
            File.WriteAllText(MessageFile, json);
        }

        if (SQL != null && Settings.UseSQL.Value)
        {
            StartSQLConnection();
        }

        Core.StartCoroutine(Clean());
    }

    private static async void StartSQLConnection()
    {
        SQLlink.InitializeBanTables();
        await Task.Yield();
        SyncDB();

        Core.StartCoroutine(SyncLoop());
    }

    public static void AddBan(Ban ban, List<Ban> list)
    {
        if (list == ChatBans)
        {
            ChatBans.Add(ban);
        }
        else if (list == VoiceBans)
        {
            VoiceBans.Add(ban);
        }
        else
        {
            Banned.Add(ban);
            if (File.Exists(Settings.BanFilePath.Value))
            {
                File.AppendAllText(Settings.BanFilePath.Value, ban.PlayerID + Environment.NewLine);
            }
        }

        if (SQL != null && Settings.UseSQL.Value)
        {
            SQLlink.AddBan(ban, list);
        }

        SaveDatabases();
    }

    public static void DeleteBan(Ban ban, List<Ban> list)
    {
        if (list == ChatBans)
        {
            ChatBans.Remove(ban);
        }
        else if (list == VoiceBans)
        {
            VoiceBans.Remove(ban);
        }
        else
        {
            Banned.Remove(ban);

            if (File.Exists(Settings.BanFilePath.Value))
            {
                var lines = File.ReadAllLines(Settings.BanFilePath.Value).ToList();
                lines.RemoveAll(line => line.Trim() == ban.PlayerID.ToString());
                File.WriteAllLines(Settings.BanFilePath.Value, lines);
            }
        }

        if (SQL != null && Settings.UseSQL.Value)
        {
            SQLlink.DeleteBan(ban, list);
        }

        SaveDatabases();
    }

    private static void SaveDatabases()
    {
        if (ChatBans.Count > 0 || File.Exists(ChatBanFile))
        {
            string json = JsonSerializer.Serialize(ChatBans, prettyJsonOptions);
            File.WriteAllText(ChatBanFile, json);
        }

        if (VoiceBans.Count > 0 || File.Exists(VoiceBanFile))
        {
            string json = JsonSerializer.Serialize(VoiceBans, prettyJsonOptions);
            File.WriteAllText(VoiceBanFile, json);
        }

        if (Banned.Count > 0 || File.Exists(BannedFile))
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
        DataTable table = SQL.Select(tableName);

        // Create a set of PlayerIDs from the database for efficient lookup
        var dbPlayerIds = new HashSet<ulong>(
            table.Rows.Cast<DataRow>()
            .Select(row => Convert.ToUInt64(row["PlayerID"]))
        );

        var removedBans = list.Where(ban => !dbPlayerIds.Contains(ban.PlayerID)).ToList();
        list.RemoveAll(ban => !dbPlayerIds.Contains(ban.PlayerID));
        if(list == Banned)
        {
            BanListFix(removedBans);
        }

        // Add missing entries from database
        foreach (DataRow row in table.Rows)
        {
            Ban ban = new Ban(
                row["PlayerName"].ToString(),
                Convert.ToUInt64(row["PlayerID"]),
                Convert.ToDateTime(row["TimeUntil"]),
                row["Reason"].ToString(),
                row["IssuedBy"].ToString()
            );

            ban.Issued = Convert.ToDateTime(row["Issued"]);
            ban.DatabaseId = Convert.ToInt32(row["DatabaseId"]);

            if (!list.Exists(x => x.PlayerID == ban.PlayerID))
            {
                list.Add(ban);

                if (list == Banned)
                {
                    if (Extensions.TryGetPlayerInfo(ban.PlayerID, out PlayerInfo player))
                    {
                        if (player.User.IsConnected)
                        {
                            Core.StartCoroutine(BanCommands.DelayKick(player));
                        }
                    }
                    if (File.Exists(Settings.BanFilePath.Value))
                        File.AppendAllText(Settings.BanFilePath.Value, ban.PlayerID.ToString() + Environment.NewLine);
                }
            }
        }

        SaveDatabases();
    }

    static IEnumerator SyncLoop()
    {
        while (true)
        {
            BanListFix();
            yield return new WaitForSeconds(Settings.SyncInterval.Value * 60 + UnityEngine.Random.Range(-10, 20));
            SyncDB();
        }
    }

    private static void BanListFix(List<Ban> additional = null)
    {
        var expiredBans = Banned
                .Where(ban => ban.TimeUntil != DateTime.MinValue && ban.TimeUntil < DateTime.Now)
                .ToList();

        if (additional != null) expiredBans = expiredBans.Concat(additional).ToList();
        foreach (var ban in expiredBans)
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

    static IEnumerator Clean()
    {
        while(true)
        {
            yield return new WaitForSeconds(60);

            foreach(var ban in Banned)
            {
                if(ban.TimeUntil == DateTime.MinValue) continue;
                if(ban.TimeUntil < DateTime.Now)
                {
                    DeleteBan(ban, Banned);
                }
            }

            foreach(var ban in ChatBans)
            {
                if(ban.TimeUntil == DateTime.MinValue) continue;
                if (ban.TimeUntil < DateTime.Now)
                {
                    DeleteBan(ban, ChatBans);
                }
            }

            foreach (var ban in VoiceBans)
            {
                if(ban.TimeUntil == DateTime.MinValue) continue;
                if (ban.TimeUntil < DateTime.Now)
                {
                    DeleteBan(ban, VoiceBans);
                }
            }
        }
    }
}