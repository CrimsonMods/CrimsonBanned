using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;

namespace CrimsonBanned.Structs;

public readonly struct Settings
{
    public static ConfigEntry<bool> ShadowBan { get; private set; }
    public static ConfigEntry<string> JSONBinAPIKey { get; private set; }
    public static ConfigEntry<string> JSONBinID { get; private set; }

    public static bool JSONBinConfigured { get; set; } = false;

    public static void InitConfig()
    {
        foreach (string path in directoryPaths)
        {
            CreateDirectories(path);
        }

        ShadowBan = InitConfigEntry("_Config", "ShadowBan", true,
            "If this is set to true, the player will never be notified that they are banned.");

        /*
        JSONBinAPIKey = InitConfigEntry("_Config", "JSONBinAPIKey", string.Empty,
            "Utilizing a JSONBin.io account, you can sync bans between servers. Consult the Thunderstore wiki on setup.");

        JSONBinID = InitConfigEntry("_Config", "JSONBinID", string.Empty,
            "The Bin ID to acess for ban information. Consult the Thunderstore wiki on setup.");

        if (!string.IsNullOrEmpty(JSONBinAPIKey.Value) && !string.IsNullOrEmpty(JSONBinID.Value))
        {
            JSONBinConfigured = true;
        }
        */
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
