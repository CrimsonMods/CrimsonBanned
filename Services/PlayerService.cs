using CrimsonBanned.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace CrimsonBanned.Services;

internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds Delay = new(60);

    static readonly ComponentType[] UserComponent = new ComponentType[]
    {
        ComponentType.ReadOnly(Il2CppType.Of<User>()),
    };

    static EntityQuery UserQuery;

    public static readonly Dictionary<string, PlayerInfo> PlayerCache = new();
    public static readonly Dictionary<string, PlayerInfo> OnlineCache = new();

    public class PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        UserQuery = EntityManager.CreateEntityQuery(UserComponent);
        Core.StartCoroutine(PlayerUpdateLoop());
    }

    static IEnumerator PlayerUpdateLoop()
    {
        while (true)
        {
            PlayerCache.Clear();
            OnlineCache.Clear();

            var players = EntityUtilities.GetEntitiesEnumerable(UserQuery);
            players
                .Select(userEntity =>
                {
                    var user = userEntity.Read<User>();
                    var playerName = user.CharacterName.Value;
                    var steamId = user.PlatformId.ToString(); // Assuming User has a SteamId property
                    var characterEntity = user.LocalCharacter._Entity;

                    return new
                    {
                        PlayerNameEntry = new KeyValuePair<string, PlayerInfo>(
                            playerName, new PlayerInfo(userEntity, characterEntity, user)),
                    };
                })
                .SelectMany(entry => new[] { entry.PlayerNameEntry })
                .GroupBy(entry => entry.Key)
                .ToDictionary(group => group.Key, group => group.First().Value)
                .ForEach(kvp =>
                {
                    PlayerCache[kvp.Key] = kvp.Value; // Add to PlayerCache

                    if (kvp.Value.User.IsConnected) // Add to OnlinePlayerCache if connected
                    {
                        OnlineCache[kvp.Key] = kvp.Value;
                    }
                });

            yield return Delay;
        }
    }
}