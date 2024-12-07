using CrimsonBanned.Structs;
using CrimsonBanned.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using System;
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
        ProcessEntities(__instance.__query_337126773_0);
        ProcessEntities(__instance.__query_337126773_1);
        ProcessEntities(__instance.__query_337126773_2);
        ProcessEntities(__instance.__query_337126773_3);
    }

    private static void ProcessEntities(EntityQuery query)
    {
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (entity.Has<FromCharacter>())
            {
                User user = entity.Read<FromCharacter>().User.Read<User>();
                HandleUser(entity, user);
            }
        }
        entities.Dispose();
    }

    private static void HandleUser(Entity entity, User user)
    {
        if (Database.VoiceBans.Exists(x => x.PlayerID == user.PlatformId))
        {
            var ban = Database.VoiceBans.First(x => x.PlayerID == user.PlatformId);

            if (DateTime.Now > ban.TimeUntil.ToLocalTime() && !TimeUtility.IsPermanent(ban.TimeUntil))
            {
                Database.DeleteBan(ban, Database.VoiceBans);
                if (!Settings.ShadowBan.Value)
                    ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user,
                        "Your voice ban has expired. Please verify in your social settings that Voice Proximity is re-enabled.");
            }
            else
            {
                if (entity.Has<VivoxEvents.ClientEvent>())
                {
                    VivoxEvents.ClientEvent clientEvent = entity.Read<VivoxEvents.ClientEvent>();
                    clientEvent.Type = VivoxRequestType.ClientLogin;

                    entity.Write(clientEvent);
                }

                if (entity.Has<VivoxEvents.ClientStateEvent>())
                {
                    VivoxEvents.ClientStateEvent clientStateEvent = entity.Read<VivoxEvents.ClientStateEvent>();
                    clientStateEvent.IsSpeaking = false;

                    entity.Write(clientStateEvent);
                }

                Core.Server.EntityManager.DestroyEntity(entity);
                return;
            }
        }

        if (Database.Banned.Exists(x => x.PlayerID == user.PlatformId))
        {
            Core.Server.EntityManager.DestroyEntity(entity);
        }
    }
}
