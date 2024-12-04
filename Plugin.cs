using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CrimsonBanned.Structs;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using VampireCommandFramework;

namespace CrimsonBanned;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("CrimsonSQL", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("CrimsonLog", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;

    public static Settings Settings;

    public static string ConfigFiles => Path.Combine(Paths.ConfigPath, "CrimsonBanned");

    public static bool LogLoaded = false;

    public override void Load()
    {
        Instance = this;
        Settings = new Settings();
        Settings.InitConfig();
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        CommandRegistry.RegisterAll();

        foreach(var plugin in IL2CPPChainloader.Instance.Plugins)
        {
            var metadata = plugin.Value.Metadata;
            if (metadata.GUID.Equals("CrimsonLog"))
            {
                LogLoaded = true;
                break;
            }
        }
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        return true;
    }

    public static void LogMessage(string message, bool console = false)
    {
        if (LogLoaded && !console)
        {
            var loggerType = Type.GetType("CrimsonLog.Systems.Logger, CrimsonLog");
            if (loggerType != null)
            {
                loggerType.GetMethod("Record").Invoke(null, new object[] { "Banned", "bans", message + "\n" });
                return;
            }
        }
        
        LogInstance.LogInfo(message);
    }
}
