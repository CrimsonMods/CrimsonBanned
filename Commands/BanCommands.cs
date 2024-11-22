﻿using CrimsonBanned.Structs;
using ProjectM;
using ProjectM.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Commands;

[CommandGroup("ban")]
internal static class BanCommands
{
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Ban(ChatCommandContext ctx, string name, int length = -1, string timeunit = "-", string reason = "")
    {
        var result = HandleBanOperation(ctx, name, length, timeunit, Database.Banned, "banned", true);
        if (result.Success) Core.StartCoroutine(DelayKick(result.PlayerInfo));
    }

    [Command(name: "chat", shortHand: "c", adminOnly: true)]
    public static void BanFromChat(ChatCommandContext ctx, string name, int length = -1, string timeunit = "-", string reason = "")
    {
        HandleBanOperation(ctx, name, length, timeunit, Database.ChatBans, "banned from chat");
    }

    [Command(name: "voice", shortHand: "v", adminOnly: true)]
    public static void BanFromVoice(ChatCommandContext ctx, string name, int length = -1, string timeunit = "-", string reason = "")
    {
        HandleBanOperation(ctx, name, length, timeunit, Database.VoiceBans, "banned from voice chat");
    }

    [Command(name: "mute", shortHand: "m", adminOnly: true)]
    public static void Mute(ChatCommandContext ctx, string name, int length = -1, string timeunit = "-", string reason = "")
    {
        BanFromChat(ctx, name, length, timeunit, reason);
        BanFromVoice(ctx, name, length, timeunit, reason);
    }

    private static (bool Success, PlayerInfo PlayerInfo) HandleBanOperation(ChatCommandContext ctx, string name, int length, string timeunit,
        List<Ban> banList, string banType, bool isGameBan = false, string reason = "")
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}.");
            return (false, null);
        }

        if(length == -1) length = Settings.DefaultBanLength.Value;
        if(timeunit == "-") timeunit = Settings.DefaultBanDenomination.Value;

        if(playerInfo.User.IsAdmin && Settings.AdminImmune.Value)
        {
            ctx.Reply("You cannot ban an admin."); return (false, null);
        }

        if (length < 0)
        {
            ctx.Reply("Please input a valid length of time.");
            return (false, null);
        }

        var timeSpan = LengthParse(length, timeunit);
        var bannedTime = DateTime.Now + timeSpan;

        if(length == 0) bannedTime = DateTime.MinValue;

        if (banList.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            ctx.Reply($"{name} is already {banType}.");
            return (false, null);
        }

        Ban ban = new Ban(playerInfo.User.CharacterName.ToString(), playerInfo.User.PlatformId, bannedTime, reason);
        Database.AddBan(ban, banList);

        ctx.Reply($"{name} has been {banType} {(length == 0 ? "permanent" : $"for {timeSpan}")}");

        if (!Settings.ShadowBan.Value || isGameBan)
        {
            var message = isGameBan ?
                $"You have been {banType} {(length == 0 ? "permanent" : $"for {timeSpan}")}. You will be kicked in 5 seconds." :
                $"You have been {banType} {(length == 0 ? "permanent" : $"for {timeSpan}")}";

            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, playerInfo.User, message);
        }

        return (true, playerInfo);
    }

    public static IEnumerator DelayKick(PlayerInfo player)
    {
        yield return new WaitForSeconds(5);

        EntityManager entityManager = Core.Server.EntityManager;
        User user = player.User;

        if (user.PlatformId == 0) yield break;

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

        if (File.Exists(Settings.BanFilePath.Value))
        {
            File.AppendAllText(Settings.BanFilePath.Value, user.PlatformId.ToString() + Environment.NewLine);
        }
    }

    private static TimeSpan LengthParse(int length, string denomination)
    {
        denomination = denomination.ToLower();

        var minuteAliases = new[] { "minute", "minutes", "min", "mins", "m" };
        var hourAliases = new[] { "hour", "hours", "hrs", "hr", "h" };
        var dayAliases = new[] { "day", "days", "d" };

        if (minuteAliases.Contains(denomination))
            denomination = "m";
        else if (hourAliases.Contains(denomination))
            denomination = "h";
        else if (dayAliases.Contains(denomination))
            denomination = "d";
        else
            denomination = "m";

        return denomination switch
        {
            "m" => TimeSpan.FromMinutes(length),
            "h" => TimeSpan.FromHours(length),
            "d" => TimeSpan.FromDays(length),
            _ => TimeSpan.FromMinutes(length)
        };
    }

}