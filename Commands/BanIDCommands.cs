using System;
using System.Collections.Generic;
using CrimsonBanned.Structs;
using CrimsonBanned.Utilities;
using VampireCommandFramework;

namespace CrimsonBanned.Commands;

[CommandGroup("banid")]
internal static class BanIDCommands
{
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Ban(ChatCommandContext ctx, ulong id, int length = 0, string timeunit = "day", string reason = "")
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleBanOperation(ctx, id, length, timeunit, Database.Banned, "banned from the server", true, reason);
        }
        else
        {
            BanCommands.Ban(ctx, playerInfo.User.CharacterName.ToString(), length, timeunit, reason);
        }
    }

    [Command(name: "chat", shortHand: "c", adminOnly: true)]
    public static void BanFromChat(ChatCommandContext ctx, ulong id, int length = 30, string timeunit = "min", string reason = "")
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleBanOperation(ctx, id, length, timeunit, Database.ChatBans, "banned from chat", false, reason);
        }
        else
        {
            BanCommands.BanFromChat(ctx, playerInfo.User.CharacterName.ToString(), length, timeunit, reason);
        }
    }

    [Command(name: "voice", shortHand: "v", adminOnly: true)]
    public static void BanFromVoice(ChatCommandContext ctx, ulong id, int length = 30, string timeunit = "min", string reason = "")
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleBanOperation(ctx, id, length, timeunit, Database.VoiceBans, "banned from voice chat", false, reason);
        }
        else
        {
            BanCommands.BanFromVoice(ctx, playerInfo.User.CharacterName.ToString(), length, timeunit, reason);
        }
    }

    [Command(name: "mute", shortHand: "m", adminOnly: true)]
    public static void Mute(ChatCommandContext ctx, ulong id, int length = 30, string timeunit = "min", string reason = "")
    {
        BanFromChat(ctx, id, length, timeunit, reason);
        BanFromVoice(ctx, id, length, timeunit, reason);
    }

    private static void HandleBanOperation(ChatCommandContext ctx, ulong id, int length, string timeunit, List<Ban> list, string banType, bool isServerBan, string reason)
    {
        var timeSpan = TimeUtility.LengthParse(length, timeunit);
        var bannedTime = DateTime.Now + timeSpan;

        if(list.Exists(x => x.PlayerID == id))
        {
            ctx.Reply($"Player {id} is already {banType}.");
            return;
        }

        if(length < 0)
        {
            ctx.Reply("Please input a valid length of time.");
            return;
        }

        if(length == 0) bannedTime = DateTime.MinValue;

        if(reason == "") reason = "(None Provided)";

        var ban = new Ban(string.Empty, id, bannedTime.ToUniversalTime(), reason, ctx.User.CharacterName.ToString());
        ban.LocalBan = true;

        if (Database.SQL != null && Settings.UseSQL.Value && SQLlink.Connect())
        {
            int response = SQLlink.AddBan(ban, list);
            if (response >= 0)
            {
                ban.DatabaseId = response;
            }
            else
            {
                int i = SQLlink.ResolveConflict(ban, list);

                if(i == 0)
                {
                    ctx.Reply($"{id} is already permanently {banType}.");
                    return;
                }

                if(i == 1)
                {
                    ctx.Reply($"{id} has had their previous ban imported from SQL with a longer duration.");
                    return;
                }

                if(i == 2)
                {
                    ctx.Reply($"{id} has been {banType} {(length == 0 ? "permanent" : $"for {TimeUtility.FormatRemainder(timeSpan)}")}");
                    return;
                }
            }
        }
        else
        {
            ban.DatabaseId = -1;
        }

        Database.AddBan(ban, list);
        ctx.Reply($"Player {id} has been {banType}.");
    }
}