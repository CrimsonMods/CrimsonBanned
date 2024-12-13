using CrimsonBanned.Structs;
using ProjectM;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VampireCommandFramework;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Commands;

[CommandGroup("unban")]
internal static class UnbanCommands
{
    [Command(name: "server", shortHand: "s", adminOnly: true)]
    public static void Unban(ChatCommandContext ctx, string name)
    {
        HandleUnbanOperation(ctx, name, Database.Banned, "unbanned from the server", "the server");
    }

    [Command(name: "chat", shortHand: "c", adminOnly: true)]
    public static void UnbanFromChat(ChatCommandContext ctx, string name)
    {
        HandleUnbanOperation(ctx, name, Database.ChatBans, "unbanned from chat", "chat");
    }

    [Command(name: "voice", shortHand: "v", adminOnly: true)]
    public static void UnbanFromVoice(ChatCommandContext ctx, string name)
    {
        HandleUnbanOperation(ctx, name, Database.VoiceBans, "unbanned from voice chat", "voice chat");
    }

    [Command(name: "mute", shortHand: "m", adminOnly: true)]
    public static void Unmute(ChatCommandContext ctx, string name)
    {
        UnbanFromChat(ctx, name);
        UnbanFromVoice(ctx, name);
    }

    private static void HandleUnbanOperation(ChatCommandContext ctx, string name, List<Ban> banList, string unbanType, string noneFound)
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}.");
            return;
        }

        if (!banList.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            ctx.Reply($"{name} is not banned from {noneFound}.");
            return;
        }

        var ban = banList.First(x => x.PlayerID == playerInfo.User.PlatformId);
        Database.DeleteBan(ban, banList);

        ctx.Reply($"{name} has been {unbanType}.");

        if (!unbanType.Contains("server") && !Settings.ShadowBan.Value) // Don't send message for game unbans
        {
            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, playerInfo.User,
                $"You have been {unbanType}.");
        }

        if (unbanType.Contains("server") && File.Exists(Settings.BanFilePath.Value))
        {
            var lines = File.ReadAllLines(Settings.BanFilePath.Value).ToList();
            lines.RemoveAll(line => line.Trim() == ban.PlayerID.ToString());
            File.WriteAllLines(Settings.BanFilePath.Value, lines);
        }
    }
}
