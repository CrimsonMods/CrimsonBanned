using BepInEx;
using BepInEx.Configuration;
using ProjectM.UI;
using System.Collections.Generic;
using System.IO;

namespace CrimsonBanned.Structs;

public readonly struct Settings
{
    public static ConfigEntry<bool> ShadowBan { get; private set; }
    public static ConfigEntry<int> DefaultBanLength { get; private set; }
    public static ConfigEntry<string> DefaultBanDenomination { get; private set; }
    public static ConfigEntry<bool> AdminImmune { get; private set; }
    public static ConfigEntry<string> BanFilePath { get; private set; }

    public static ConfigEntry<int> SyncInterval { get; private set; }

    public static void InitConfig()
    {
        foreach (string path in directoryPaths)
        {
            CreateDirectories(path);
        }

        DefaultBanDenomination = InitConfigEntry("_Config", "DefaultUnitOfTime", "minute",
        "Valid Options: day, hour, minute");
        DefaultBanLength = InitConfigEntry("_Config", "DefaultBanLength", 30,
        "The length of the chosen unit of time to apply the ban. 0 will default to perma-bans.");
        ShadowBan = InitConfigEntry("_Config", "ShadowBan", true,
            "If this is set to true, the player will never be notified that they are banned.");
        AdminImmune = InitConfigEntry("_Config", "AdminImmune", true,
            "If this is set to true, admins will be immune to bans.");
        BanFilePath = InitConfigEntry("_Config", "BanFilePath", "save-data/Settings/banlist.txt",
            "The path from root to the banlist.txt file");

        if (Database.SQL != null)
        {
            SyncInterval = InitConfigEntry("ServerConnection", "SyncInterval", 60,
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
