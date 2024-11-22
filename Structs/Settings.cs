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

    public static ConfigEntry<string> MySQLDbName { get; private set; }
    public static ConfigEntry<string> Host { get; private set; }
    public static ConfigEntry<int> Port { get; private set; }
    public static ConfigEntry<string> UserName { get; private set; }
    public static ConfigEntry<string> Password { get; private set; }
    public static ConfigEntry<int> SyncInterval { get; private set; }

    public static bool MySQLConfigured { get; set; } = false;

    public static void InitConfig()
    {
        foreach (string path in directoryPaths)
        {
            CreateDirectories(path);
        }

        DefaultBanDenomination = InitConfigEntry("_Config", "DefaultBanDenomination", "minute",
        "Valid Options: day, hour, minute");
        DefaultBanLength = InitConfigEntry("_Config", "DefaultBanLength", 30,
        "The length of the chosen denomination to apply the ban. 0 will default to perma-bans.");
        ShadowBan = InitConfigEntry("_Config", "ShadowBan", true,
            "If this is set to true, the player will never be notified that they are banned.");
        AdminImmune = InitConfigEntry("_Config", "AdminImmune", true,
            "If this is set to true, admins will be immune to bans.");

        // MySQL DB
        MySQLDbName = InitConfigEntry("ServerConnection", "MySQLDbName", "",
            "The name of your MySQL database.");
        Host = InitConfigEntry("ServerConnection", "Host", "",
            "The host address of your MySQL database.");
        Port = InitConfigEntry("ServerConnection", "Port", 3306,
            "The port of your database server.");
        UserName = InitConfigEntry("ServerConnection", "Username", "",
            "The login username for your database.");
        Password = InitConfigEntry("ServerConnection", "Password", "",
            "The login password for your database.");

        SyncInterval = InitConfigEntry("ServerConnection", "SyncInterval", 60,
            "The interval in minutes to sync the database.");

        if (
            !string.IsNullOrEmpty(MySQLDbName.Value)
            && !string.IsNullOrEmpty(Host.Value)
            && Port.Value != 0
            && !string.IsNullOrEmpty(UserName.Value)
            && !string.IsNullOrEmpty(Password.Value)
          ) MySQLConfigured = true;
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
