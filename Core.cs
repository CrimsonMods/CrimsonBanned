using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using CrimsonBanned.Services;
using CrimsonBanned.Structs;
using ProjectM.Physics;
using ProjectM.Scripting;
using System;
using System.Collections;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace CrimsonBanned;

internal static class Core
{
    public static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world (yet)...");
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerGameManager ServerGameManager => SystemService.ServerScriptMapper.GetServerGameManager();
    public static SystemService SystemService { get; } = new(Server);
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;
    public static Database Database;
    public static PlayerService PlayerService;

    static MonoBehaviour MonoBehaviour;

    public static bool hasInitialized = false;
    public static void Initialize()
    {
        if (hasInitialized) return;

        PlayerService = new PlayerService();

        Database = new Database();

        hasInitialized = true;
    }

    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }

    public static void StartCoroutine(IEnumerator routine)
    {
        if (MonoBehaviour == null)
        {
            MonoBehaviour = new GameObject("CrimsonBanned").AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(MonoBehaviour.gameObject);
        }
        MonoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
}
