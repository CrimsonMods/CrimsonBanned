using CrimsonBanned.Structs;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using System;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace CrimsonBanned.Patches;

[HarmonyPatch]
public static class VivoxPatch
{
    [HarmonyPatch(typeof(VivoxConnectionSystem), nameof(VivoxConnectionSystem.OnUpdate))]
    public static void Prefix(VivoxConnectionSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_337126773_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entity.Has<FromCharacter>())
            {
                User user = entity.Read<FromCharacter>().User.Read<User>();
                if (Database.VoiceBans.Exists(x => x.PlayerID == user.PlatformId))
                {
                    // check if expired

                    Core.Server.EntityManager.DestroyEntity(entity);
                }
            }
        }

        NativeArray<Entity> entities1 = __instance.__query_337126773_1.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities1)
        {
            if (entity.Has<FromCharacter>())
            {
                User user = entity.Read<FromCharacter>().User.Read<User>();
                if (Database.VoiceBans.Exists(x => x.PlayerID == user.PlatformId))
                {
                    var ban = Database.VoiceBans.First(x => x.PlayerID == user.PlatformId);

                    if (DateTime.Now > ban.TimeUntil)
                    {
                        Database.VoiceBans.Remove(ban);

                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                            "Your voice ban has expired. Please verify in your social settings that Voice Proximity is re-enabled.");
                    }
                    else
                    {
                        Core.Server.EntityManager.DestroyEntity(entity);
                    }
                }
            }
        }

        NativeArray<Entity> entities2 = __instance.__query_337126773_2.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities2)
        {
            if (entity.Has<FromCharacter>())
            {
                User user = entity.Read<FromCharacter>().User.Read<User>();
                if (Database.VoiceBans.Exists(x => x.PlayerID == user.PlatformId))
                {
                    Core.Server.EntityManager.DestroyEntity(entity);
                }
            }
        }

        NativeArray<Entity> entities3 = __instance.__query_337126773_3.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities3)
        {
            if (entity.Has<FromCharacter>())
            {
                User user = entity.Read<FromCharacter>().User.Read<User>();
                if (Database.VoiceBans.Exists(x => x.PlayerID == user.PlatformId))
                {
                    Core.Server.EntityManager.DestroyEntity(entity);
                }
            }
        }
    }

    public static void EntityCompomponentDumper(string filePath, Entity entity)
    {
        File.AppendAllText(filePath, $"--------------------------------------------------" + Environment.NewLine);
        File.AppendAllText(filePath, $"Dumping components of {entity.ToString()}:" + Environment.NewLine);

        foreach (var componentType in Core.Server.EntityManager.GetComponentTypes(entity))
        { File.AppendAllText(filePath, $"{componentType.ToString()}" + Environment.NewLine); }

        File.AppendAllText(filePath, $"--------------------------------------------------" + Environment.NewLine);

        File.AppendAllText(filePath, DumpEntity(entity));
    }

    private static string DumpEntity(Entity entity, bool fullDump = true)
    {
        var sb = new Il2CppSystem.Text.StringBuilder();
        ProjectM.EntityDebuggingUtility.DumpEntity(Core.Server, entity, fullDump, sb);
        return sb.ToString();
    }
}
