using CrimsonBanned.Commands;
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
using CrimsonBanned.Utilities;
using System.ComponentModel;

namespace CrimsonBanned.Structs;

internal class Database
{
    public static readonly JsonSerializerOptions prettyJsonOptions = new()
    {
        WriteIndented = true,
        IncludeFields = true,
    };
    public static string BannedFile = Path.Combine(Plugin.ConfigFiles, "bans_server.json");
    public static string ChatBanFile = Path.Combine(Plugin.ConfigFiles, "bans_chat.json");
    public static string VoiceBanFile = Path.Combine(Plugin.ConfigFiles, "bans_voice.json");
    public static string MessageFile = Path.Combine(Plugin.ConfigFiles, "messages.json");
    public static string DeleteFile = Path.Combine(Plugin.ConfigFiles, "ignore.json");

    public static List<Ban> Banned;
    public static List<Ban> ChatBans;
    public static List<Ban> VoiceBans;
    public static List<MessagePair> Messages;
    public static List<DeleteLater> Deletes;

    public static dynamic SQL => IL2CPPChainloader.Instance.Plugins.TryGetValue("CrimsonSQL", out var pluginInfo)
        ? pluginInfo.Instance.GetType().GetProperty("SQLService").GetValue(pluginInfo.Instance)
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

        if (File.Exists(DeleteFile))
        {
            string json = File.ReadAllText(DeleteFile);
            Deletes = JsonSerializer.Deserialize<List<DeleteLater>>(json, prettyJsonOptions);
        }
        else
        {
            Deletes = new List<DeleteLater>();
        }

        if (File.Exists(MessageFile))
        {
            string json = File.ReadAllText(MessageFile);
            Messages = JsonSerializer.Deserialize<List<MessagePair>>(json, prettyJsonOptions);
        }
        else
        {
            Messages =
            [
                new MessagePair("CheckHeader", "\n{player}'s ({id}) Bans:"),
                new MessagePair("CheckBanLine", "\n{type} Ban\nIssued: {issued}\nRemaining: {remainder}\nReason: {reason}"),
                new MessagePair("ListBan", "\n{player} ({id}) - {remainder}")
            ];

            string json = JsonSerializer.Serialize(Messages, prettyJsonOptions);
            File.WriteAllText(MessageFile, json);
        }

        if (SQL != null && Settings.UseSQL.Value && SQLlink.Connect())
        {
            StartSQLConnection();
        }

        Core.StartCoroutine(Clean());
    }

    private static async void StartSQLConnection()
    {
        SQLlink.InitializeBanTables();
        await Task.Yield();
        SQLUtility.SyncDB();

        Core.StartCoroutine(SyncLoop());
    }

    public static void AddBan(Ban ban, List<Ban> list)
    {
        if (list.Exists(x => x.DatabaseId == ban.DatabaseId))
        {
            return;
        }

        string type = string.Empty;
        if (list == ChatBans)
        {
            type = "Chat";
            ChatBans.Add(ban);
        }
        else if (list == VoiceBans)
        {
            type = "Voice";
            VoiceBans.Add(ban);
        }
        else
        {
            type = "Server";
            Banned.Add(ban);
            if (File.Exists(Settings.BanFilePath.Value))
            {
                var existingBans = File.ReadAllLines(Settings.BanFilePath.Value);
                if (!existingBans.Contains(ban.PlayerID.ToString()))
                {
                    File.AppendAllText(Settings.BanFilePath.Value, ban.PlayerID + Environment.NewLine);
                }
            }
        }

        string log = string.Empty;
        string length = TimeUtility.FormatRemainder(ban.TimeUntil.ToLocalTime());
        if (ban.LocalBan)
        {
            log = $"{ban.PlayerName} with ID: {ban.PlayerID} has been {type} banned. Issued by {ban.IssuedBy} for {length}.";
        }
        else
        {
            log = $"A player with ID: {ban.PlayerID} has had their {type} ban synced from SQL. Originally issued by {ban.IssuedBy} with {length} remaining.";
        }

        Plugin.LogMessage(log);
        SaveDatabases();
    }

    public static void DeleteBan(Ban ban, List<Ban> list)
    {
        string type = string.Empty;
        if (list == ChatBans)
        {
            type = "Chat";
            ChatBans.Remove(ban);
        }
        else if (list == VoiceBans)
        {
            type = "Voice";
            VoiceBans.Remove(ban);
        }
        else
        {
            type = "Server";
            Banned.Remove(ban);

            if (File.Exists(Settings.BanFilePath.Value))
            {
                var lines = File.ReadAllLines(Settings.BanFilePath.Value).ToList();
                lines.RemoveAll(line => line.Trim() == ban.PlayerID.ToString());
                File.WriteAllLines(Settings.BanFilePath.Value, lines);
            }
        }

        string log = string.Empty;
        if (ban.LocalBan)
        {
            log = $"{type} ban has ended for {ban.PlayerName}.";
        }
        else
        {
            log = $"{type} ban has ended for synced player ID: {ban.PlayerID}.";
        }

        Plugin.LogMessage(log);

        if (SQLUtility.TrySQL())
        {
            SQLlink.DeleteBan(ban, list);
        }
        else if (SQL != null) // SQL exists but connection failed
        {
            Deletes.Add(new DeleteLater(ban.DatabaseId, $"{type}Bans"));
        }

        SaveDatabases();
    }

    public static void SaveDatabases()
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

        if (Deletes.Count > 0 || File.Exists(DeleteFile))
        {
            string json = JsonSerializer.Serialize(Deletes, prettyJsonOptions);
            File.WriteAllText(DeleteFile, json);
        }
    }

    static IEnumerator SyncLoop()
    {
        while (true)
        {
            BanListFix();
            yield return new WaitForSeconds(Settings.SyncInterval.Value * 60 + UnityEngine.Random.Range(-10, 20));
            SQLUtility.SyncDB();
        }
    }

    public static void BanListFix(List<Ban> additional = null)
    {
        var expiredBans = Banned
                .Where(ban => !TimeUtility.IsPermanent(ban.TimeUntil) && ban.TimeUntil < DateTime.UtcNow)
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
        while (true)
        {
            yield return new WaitForSeconds(60);

            var bannedToRemove = Banned.Where(ban => !TimeUtility.IsPermanent(ban.TimeUntil) && ban.TimeUntil < DateTime.UtcNow).ToList();
            var chatBansToRemove = ChatBans.Where(ban => !TimeUtility.IsPermanent(ban.TimeUntil) && ban.TimeUntil < DateTime.UtcNow).ToList();
            var voiceBansToRemove = VoiceBans.Where(ban => !TimeUtility.IsPermanent(ban.TimeUntil) && ban.TimeUntil < DateTime.UtcNow).ToList();

            foreach (var ban in bannedToRemove)
            {
                DeleteBan(ban, Banned);
            }

            foreach (var ban in chatBansToRemove)
            {
                DeleteBan(ban, ChatBans);
            }

            foreach (var ban in voiceBansToRemove)
            {
                DeleteBan(ban, VoiceBans);
            }
        }
    }
}

public struct DeleteLater
{
    public int ID { get; set; }
    public string TableName { get; set; }

    public DeleteLater(int id, string tableName)
    {
        ID = id;
        TableName = tableName;
    }
}