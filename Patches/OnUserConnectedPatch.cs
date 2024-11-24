using CrimsonBanned.Structs;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using System;
using System.Linq;
using Unity.Entities;

namespace CrimsonBanned.Patches;

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
public static class OnUserConnectedPatcch
{
    public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        try
        {
            var em = __instance.EntityManager;
            var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
            var serverClient = __instance._ApprovedUsersLookup[userIndex];
            var userEntity = serverClient.UserEntity;
            var userData = __instance.EntityManager.GetComponentData<User>(userEntity);
            bool isNewVampire = userData.CharacterName.IsEmpty;

            if (Database.Banned.Exists(x => x.PlayerID == userData.PlatformId))
            {
                var ban = Database.Banned.First(x => x.PlayerID == userData.PlatformId);

                if (DateTime.Now > ban.TimeUntil)
                {
                    Database.DeleteBan(ban, Database.Banned);
                }
                else
                {
                    Entity entity = Core.EntityManager.CreateEntity(new ComponentType[3]
                    {
                        ComponentType.ReadOnly<NetworkEventType>(),
                        ComponentType.ReadOnly<SendEventToUser>(),
                        ComponentType.ReadOnly<KickEvent>()
                    });

                    entity.Write(new KickEvent()
                    {
                        PlatformId = userData.PlatformId
                    });
                    entity.Write(new SendEventToUser()
                    {
                        UserIndex = userData.Index
                    });
                    entity.Write(new NetworkEventType()
                    {
                        EventId = NetworkEvents.EventId_KickEvent,
                        IsAdminEvent = false,
                        IsDebugEvent = false
                    });
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Failure in {nameof(ServerBootstrapSystem.OnUserConnected)}\nMessage: {e.Message} Inner:{e.InnerException?.Message}\n\nStack: {e.StackTrace}\nInner Stack: {e.InnerException?.StackTrace}");
        }
    }
}