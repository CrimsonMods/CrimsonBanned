using CrimsonBanned.Structs;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace CrimsonBanned.Patches;

[HarmonyPatch]
public static class ChatMessagePatch
{
    [HarmonyPatch(typeof(ChatMessageSystem), nameof(ChatMessageSystem.OnUpdate))]
    public static bool Prefix(ChatMessageSystem __instance)
    {
        if (__instance.__query_661171423_0 != null)
        {
            NativeArray<Entity> entities = __instance.__query_661171423_0.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var fromData = __instance.EntityManager.GetComponentData<FromCharacter>(entity);
                var userData = __instance.EntityManager.GetComponentData<User>(fromData.User);
                var chatEventData = __instance.EntityManager.GetComponentData<ChatMessageEvent>(entity);

                var messageText = chatEventData.MessageText.ToString();

                if (chatEventData.MessageType == ChatMessageType.System) continue;

                if (Database.ChatBans.Exists(x => x.PlayerID == userData.PlatformId))
                {
                    var ban = Database.ChatBans.First(x => x.PlayerID == userData.PlatformId);

                    if (DateTime.Now > ban.TimeUntil)
                    {
                        Database.ChatBans.Remove(ban);
                        Database.SaveDatabases();

                        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, userData, "Your chat ban has ended.");
                    }
                    else
                    {
                        Core.Server.EntityManager.DestroyEntity(entity);
                    }
                }
            }
        }

        return true;
    }
}
