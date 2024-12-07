using CrimsonBanned.Structs;
using CrimsonBanned.Utilities;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Commands;

[CommandGroup("ban")]
internal static class BanCommands
{
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Ban(ChatCommandContext ctx, string name, int length = 0, string timeunit = "day", string reason = "")
    {
        var result = HandleBanOperation(ctx, name, length, timeunit, Database.Banned, "banned from the server", true, reason);
        if (result.Success)
        {
            Core.StartCoroutine(DelayKick(result.PlayerInfo));
        }
    }

    /*  I wish VCF supported overloading commands
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Ban(ChatCommandContext ctx, string name, string reason = "")
    {
        var result = HandleBanOperation(ctx, name, 0, "day", Database.Banned, "banned", true, reason);
        if(result.Success) Core.StartCoroutine(DelayKick(result.PlayerInfo));
    }
    */

    [Command(name: "chat", shortHand: "c", adminOnly: true)]
    public static void BanFromChat(ChatCommandContext ctx, string name, int length = 30, string timeunit = "min", string reason = "")
    {
        HandleBanOperation(ctx, name, length, timeunit, Database.ChatBans, "banned from chat", false, reason);
    }

    [Command(name: "voice", shortHand: "v", adminOnly: true)]
    public static void BanFromVoice(ChatCommandContext ctx, string name, int length = 30, string timeunit = "min", string reason = "")
    {
        HandleBanOperation(ctx, name, length, timeunit, Database.VoiceBans, "banned from voice chat", false, reason);
    }

    [Command(name: "mute", shortHand: "m", adminOnly: true)]
    public static void Mute(ChatCommandContext ctx, string name, int length = 30, string timeunit = "min", string reason = "")
    {
        BanFromChat(ctx, name, length, timeunit, reason);
        BanFromVoice(ctx, name, length, timeunit, reason);
    }

    [Command(name: "kick", shortHand: "k", adminOnly: true)]
    public static void KickPlayer(ChatCommandContext ctx, string name)
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}.");
            return;
        }

        if (playerInfo.User == ctx.User)
        {
            ctx.Reply("You cannot kick yourself.");
            return;
        }

        Kick(playerInfo);
    }

    private static (bool Success, PlayerInfo PlayerInfo) HandleBanOperation(ChatCommandContext ctx, string name, int length, string timeunit,
        List<Ban> banList, string banType, bool isGameBan = false, string reason = "")
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}.");
            return (false, null);
        }

        if (playerInfo.User.IsAdmin)
        {
            ctx.Reply("You cannot ban an admin."); return (false, null);
        }

        if (playerInfo.User == ctx.User)
        {
            ctx.Reply("You cannot ban yourself.");
            return (false, null);
        }

        if (length < 0)
        {
            ctx.Reply("Please input a valid length of time.");
            return (false, null);
        }

        var timeSpan = TimeUtility.LengthParse(length, timeunit);
        var bannedTime = DateTime.Now + timeSpan;

        if (reason == "") reason = "";

        if (length == 0) bannedTime = DateTime.MinValue;

        if (banList.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            ctx.Reply($"{name} is already {banType}.");
            return (false, null);
        }

        Ban ban = new Ban(playerInfo.User.CharacterName.ToString(), playerInfo.User.PlatformId, bannedTime.ToUniversalTime(), reason, ctx.User.CharacterName.ToString());
        ban.LocalBan = true;

        if (Database.SQL != null && Settings.UseSQL.Value && SQLlink.Connect())
        {
            int response = SQLlink.AddBan(ban, banList);
            if (response >= 0)
            {
                ban.DatabaseId = response;
            }
            else
            {
                int i = SQLlink.ResolveConflict(ban, banList);

                if(i == 0)
                {
                    ctx.Reply($"{name} is already permanently {banType}.");
                    return (false, null);
                }

                if(i == 1)
                {
                    ctx.Reply($"{name} has had their previous ban imported from SQL with a longer duration.");
                    return (true, playerInfo);
                }

                if(i == 2)
                {
                    ctx.Reply($"{name} has been {banType} {(length == 0 ? "permanent" : $"for {TimeUtility.FormatRemainder(timeSpan)}")}");
                    return (true, playerInfo);
                }
            }
        }
        else
        {
            ban.DatabaseId = -1;
        }

        Database.AddBan(ban, banList);

        ctx.Reply($"{name} has been {banType} {(length == 0 ? "permanently" : $"for {TimeUtility.FormatRemainder(timeSpan)}")}");

        if (!Settings.ShadowBan.Value || isGameBan)
        {
            var message = isGameBan ?
                $"You have been {banType} {(length == 0 ? "permanently." : $"for {TimeUtility.FormatRemainder(timeSpan)}")}. You will be kicked in 5 seconds." :
                $"You have been {banType} {(length == 0 ? "permanently." : $"for {TimeUtility.FormatRemainder(timeSpan)}")}.";

            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, playerInfo.User, message);
        }

        return (true, playerInfo);
    }

    public static IEnumerator DelayKick(PlayerInfo player)
    {
        yield return new WaitForSeconds(5);

        Kick(player);
    }

    private static void Kick(PlayerInfo player)
    {
        EntityManager entityManager = Core.Server.EntityManager;
        User user = player.User;

        if (user.PlatformId == 0) return;

        Entity entity = entityManager.CreateEntity(new ComponentType[3]
        {
            ComponentType.ReadOnly<NetworkEventType>(),
            ComponentType.ReadOnly<SendEventToUser>(),
            ComponentType.ReadOnly<KickEvent>()
        });
        entity.Write(new KickEvent()
        {
            PlatformId = user.PlatformId
        });
        entity.Write(new SendEventToUser()
        {
            UserIndex = user.Index
        });
        entity.Write(new NetworkEventType()
        {
            EventId = NetworkEvents.EventId_KickEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        });
    }
}