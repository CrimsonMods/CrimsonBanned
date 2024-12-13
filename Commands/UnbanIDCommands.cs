using CrimsonBanned.Structs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VampireCommandFramework;

namespace CrimsonBanned.Commands;

[CommandGroup("unbanid")]
internal static class UnbanIDCommands
{
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Unban(ChatCommandContext ctx, ulong id)
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleUnbanOperation(ctx, id, Database.Banned, "unbanned from the server", "the server");
        }
        else
        {
            UnbanCommands.Unban(ctx, playerInfo.User.CharacterName.ToString());
        }
    }

    [Command(name: "chat", shortHand: "c", adminOnly: true)]
    public static void UnbanFromChat(ChatCommandContext ctx, ulong id)
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleUnbanOperation(ctx, id, Database.ChatBans, "unbanned from chat", "chat");
        }
        else
        {
            UnbanCommands.UnbanFromChat(ctx, playerInfo.User.CharacterName.ToString());
        }
    }

    [Command(name: "voice", shortHand: "v", adminOnly: true)]
    public static void UnbanFromVoice(ChatCommandContext ctx, ulong id)
    {
        if(!Extensions.TryGetPlayerInfo(id, out var playerInfo))
        {
            HandleUnbanOperation(ctx, id, Database.VoiceBans, "unbanned from voice chat", "voice chat");
        }
        else
        {
            UnbanCommands.UnbanFromVoice(ctx, playerInfo.User.CharacterName.ToString());
        }
    }

    [Command(name: "mute", shortHand: "m", adminOnly: true)]
    public static void Unmute(ChatCommandContext ctx, ulong id)
    {
        UnbanFromChat(ctx, id);
        UnbanFromVoice(ctx, id);
    }

    private static void HandleUnbanOperation(ChatCommandContext ctx, ulong id, List<Ban> banList, string unbanType, string noneFound)
    {
        if (!banList.Exists(x => x.PlayerID == id))
        {
            ctx.Reply($"{id} is not banned from {noneFound}.");
            return;
        }

        var ban = banList.First(x => x.PlayerID == id);
        Database.DeleteBan(ban, banList);

        ctx.Reply($"{id} has been {unbanType}.");

        if (unbanType.Contains("server") && File.Exists(Settings.BanFilePath.Value))
        {
            var lines = File.ReadAllLines(Settings.BanFilePath.Value).ToList();
            lines.RemoveAll(line => line.Trim() == ban.PlayerID.ToString());
            File.WriteAllLines(Settings.BanFilePath.Value, lines);
        }
    }
}