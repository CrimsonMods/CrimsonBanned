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
public class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; private set; }
    public static Harmony Harmony => Instance._harmony;
    public static ManualLogSource LogInstance => Instance.Log;
    public static Settings Settings;

    public static string ConfigFiles => Path.Combine(Paths.ConfigPath, "CrimsonBanned");

    public override void Load()
    {
        Instance = this;
        Settings = new Settings();
        Settings.InitConfig();
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        CommandRegistry.RegisterAll();
    }

    public override bool Unload()
    {
        _harmony?.UnpatchSelf();
        return true;
    }
}
