using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrimsonBanned.Structs;
using VampireCommandFramework;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Commands;

[CommandGroup("banned")]
internal static class BannedCommands
{
    const int MESSAGE_LIMIT = 508 - 26 - 2 - 20;

    [Command(name: "list", shortHand: "l", adminOnly: true)]
    public static void List(ChatCommandContext ctx, string listName = "server")
    {
        List<Ban> bans = listName switch
        {
            "server" => Database.Banned,
            "s" => Database.Banned,
            "chat" => Database.ChatBans,
            "c" => Database.ChatBans,
            "voice" => Database.VoiceBans,
            "v" => Database.VoiceBans,
            _ => []
        };

        string s = "";

        List<string> messages = new() { s };
        int currentMessageIndex = 0;

        foreach (var user in bans)
        {
            s += Database.Messages[2].ToString(user, null);

            if (messages[currentMessageIndex].Length + s.Length > MESSAGE_LIMIT)
            {
                currentMessageIndex++;
                messages.Add(s);
            }
            else
            {
                messages[currentMessageIndex] += s;
            }
        }

        if (bans.Count == 0)
        {
            ctx.Reply("No players in this ban list.");
            return;
        }

        foreach (var message in messages) ctx.Reply(message);
    }

    [Command(name: "loaduntracked", adminOnly: true)]
    public static void Backlog(ChatCommandContext ctx)
    {
        if (Database.SQL == null)
        {
            ctx.Reply("MySQL is not configured. Please configure MySQL using CrimsonSQL.");
            return;
        }

        Database.SyncDB();

        if (File.Exists(Settings.BanFilePath.Value))
        {
            string[] content = File.ReadAllLines(Settings.BanFilePath.Value);

            foreach (string line in content)
            {
                if (Database.Banned.Exists(x => x.PlayerID == Convert.ToUInt64(line))) continue;

                Ban ban = new Ban(string.Empty, Convert.ToUInt64(line), DateTime.MinValue, "Synced from local banlist.txt file.", "banlist.txt");
                ban.LocalBan = true;
                Database.AddBan(ban, Database.Banned);
            }

            ctx.Reply("Local banlist.txt synced to MySQL database.");
        }
        else
        {
            ctx.Reply("Local banlist.txt not found. Please specify the file path.");
        }
    }

    [Command(name: "sync", adminOnly: true)]
    public static void Sync(ChatCommandContext ctx)
    {
        if (Database.SQL == null)
        {
            ctx.Reply("MySQL is not configured. Please configure MySQL using CrimsonSQL.");
            return;
        }

        Database.SyncDB();
    }

    [Command(name: "checkid", adminOnly: true)]
    public static void CheckID(ChatCommandContext ctx, string id)
    {
        if (!Extensions.TryGetPlayerInfo(id, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the ID {id}");
            return;
        }

        Check(ctx, playerInfo.User.CharacterName.ToString());
    }

    [Command(name: "check", adminOnly: true)]
    public static void Check(ChatCommandContext ctx, string name)
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}");
            return;
        }

        List<(Ban, BanDetails)> playerBans = new List<(Ban, BanDetails)>();

        List<Ban> allBans = new List<Ban>
        {
            Database.ChatBans.Find(x => x.PlayerID == playerInfo.User.PlatformId),
            Database.VoiceBans.Find(x => x.PlayerID == playerInfo.User.PlatformId),
            Database.Banned.Find(x => x.PlayerID == playerInfo.User.PlatformId)
        };

        foreach (var ban in allBans)
        {
            if (ban != null)
            {
                var list = ban switch
                {
                    var b when Database.Banned.Contains(b) => Database.Banned,
                    var b when Database.ChatBans.Contains(b) => Database.ChatBans,
                    var b when Database.VoiceBans.Contains(b) => Database.VoiceBans,
                    _ => null
                };

                if (list != null && GetBanDetails(ban, list, out BanDetails details))
                {
                    playerBans.Add((ban, details));
                }
            }
        }

        if (playerBans.Count == 0)
        {
            ctx.Reply($"{name} is not banned.");
            return;
        }

        StringBuilder banList = new StringBuilder();
        banList.AppendLine(Database.Messages[0].ToString(playerBans[0].Item1, playerBans[1].Item2));
        foreach (var ban in playerBans)
        {
            banList.AppendLine(Database.Messages[1].ToString(ban.Item1, ban.Item2));
        }

        ctx.Reply(banList.ToString());
    }

    internal static bool GetBanDetails(Ban ban, List<Ban> list, out BanDetails details)
    {
        if (DateTime.Now > ban.TimeUntil)
        {
            details = null;
            Database.DeleteBan(ban, list);
            return false;
        }

        details = new BanDetails();
        details.Ban = ban;
        details.IssuedOn = ban.Issued.ToString("MM/dd/yy HH:mm");

        switch (list)
        {
            case var _ when list == Database.Banned:
                details.BanType = "Server";
                break;
            case var _ when list == Database.ChatBans:
                details.BanType = "Chat";
                break;
            case var _ when list == Database.VoiceBans:
                details.BanType = "Voice";
                break;
        }

        if (ban.TimeUntil == DateTime.MinValue)
        {
            details.RemainingTime = "Permanent";
        }
        else
        {
            details.RemainingTime = $"Expires in {ban.TimeUntil - DateTime.Now}";
        }

        return true;
    }
}

public class BanDetails
{
    public Ban Ban { get; set; }
    public string BanType { get; set; }
    public string IssuedOn { get; set; }
    public string RemainingTime { get; set; }
}
