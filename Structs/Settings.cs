using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;

namespace CrimsonBanned.Structs;

public readonly struct Settings
{
    private const string ConfigHeader = "_Config";
    private const string ServerHeader = "ServerConnection";
    public static ConfigEntry<bool> ShadowBan { get; private set; }
    public static ConfigEntry<string> BanFilePath { get; private set; }

    public static ConfigEntry<bool> UseSQL { get; private set; }
    public static ConfigEntry<int> SyncInterval { get; private set; }

    public static void InitConfig()
    {
        foreach (string path in directoryPaths)
        {
            CreateDirectories(path);
        }

        ShadowBan = InitConfigEntry(ConfigHeader, "ShadowBan", false,
            "If this is set to true, the player will never be notified when chat or voice banned.");
        BanFilePath = InitConfigEntry(ConfigHeader, "BanFilePath", "save-data/Settings/banlist.txt",
            "The path from root to the banlist.txt file");

        if (Database.SQL != null)
        {
            UseSQL = InitConfigEntry(ServerHeader, "UseSQL", false,
                "If this is set to true, the plugin will use CrimsonSQL to store bans.");
            SyncInterval = InitConfigEntry(ServerHeader, "SyncInterval", 60,
                "The interval in minutes to sync the database.");
        }
    }

    static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
    {
        // Bind the configuration entry and get its value
        var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

        // Check if the key exists in the configuration file and retrieve its current value
        var newFile = Path.Combine(Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

        if (File.Exists(newFile))
        {
            var config = new ConfigFile(newFile, true);
            if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry))
            {
                // If the entry exists, update the value to the existing value
                entry.Value = existingEntry.Value;
            }
        }
        return entry;
    }

    static void CreateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    static readonly List<string> directoryPaths =
        [
            Plugin.ConfigFiles
        ];
}
