using CrimsonBanned.Services;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

    public Database()
    {
        LoadDatabases();
    }

    private static async void LoadDatabases()
    {
        if (Settings.JSONBinConfigured)
        {
            BansContainer container = await JSONBinService.GetBans();

            if (container != null)
            {
                ChatBans = container.ChatBans;
                VoiceBans = container.VoiceBans;
                return;
            }
            else
            {
                Plugin.LogInstance.LogError("Failed to load database. Defaulting to local files.");
                Settings.JSONBinConfigured = false;
            }
        }

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
    }

    public static async void SaveDatabases()
    {
        if (Settings.JSONBinConfigured)
        {
            if (await JSONBinService.UpdateBans(new BansContainer(ChatBans, VoiceBans)))
            {
                return;
            }
        }

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
}
