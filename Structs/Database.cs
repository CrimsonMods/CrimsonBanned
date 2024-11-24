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

    public static List<Ban> Banned;
    public static List<Ban> ChatBans;
    public static List<Ban> VoiceBans;

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

        if(SQL != null)
        {
            StartSQLConnection();
        }
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
        }

        if(SQL != null)
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

        if(SQL != null)
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
        foreach (DataRow row in table.Rows)
        {
            Ban ban = new Ban(
                row["PlayerName"].ToString(),
                Convert.ToUInt64(row["PlayerID"]),
                Convert.ToDateTime(row["TimeUntil"]),
                row["Reason"].ToString()
            );

            if (!list.Exists(x => x.PlayerID == ban.PlayerID))
            {
                list.Add(ban);

                if (list == Banned)
                {
                    if (File.Exists(Settings.BanFilePath.Value))
                    {
                        if (Extensions.TryGetPlayerInfo(ban.PlayerID, out PlayerInfo player))
                        {
                            if (player.User.IsConnected)
                            {
                                Core.StartCoroutine(BanCommands.DelayKick(player));
                            }
                        }
                        File.AppendAllText(Settings.BanFilePath.Value, ban.PlayerID.ToString() + Environment.NewLine);
                    }
                }
            }
        }
    }

    static IEnumerator SyncLoop()
    {
        while (true)
        {
            var expiredBans = Banned
                .Where(ban => ban.TimeUntil != DateTime.MinValue && ban.TimeUntil < DateTime.Now)
                .ToList();

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

            yield return new WaitForSeconds(Settings.SyncInterval.Value * 60 + UnityEngine.Random.Range(-10, 20));
            SyncDB();
        }
    }
}