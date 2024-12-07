using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrimsonBanned.Structs;
using CrimsonBanned.Utilities;
using VampireCommandFramework;
using static CrimsonBanned.Services.PlayerService;

namespace CrimsonBanned.Commands;

[CommandGroup("banned")]
internal static class BannedCommands
{
    const int MESSAGE_LIMIT = 508 - 26 - 2 - 20;
    const int MESSAGES_PER_PAGE = 2;

    [Command(name: "list", shortHand: "l", adminOnly: true)]
    public static void List(ChatCommandContext ctx, string banType = "server", string show = "perma/temp", int page = 1)
    {
        banType = banType.ToLower() switch
        {
            "server" => "Server",
            "s" => "Server",
            "chat" => "Chat",
            "c" => "Chat",
            "voice" => "Voice",
            "v" => "Voice",
            _ => "Server"
        };

        List<Ban> bans = banType switch
        {
            "Server" => Database.Banned,
            "Chat" => Database.ChatBans,
            "Voice" => Database.VoiceBans,
            _ => []
        };

        show = show switch
        {
            "perma" => "perma",
            "temp" => "temp",
            "p" => "perma",
            "t" => "temp",
            _ => "perma/temp"
        };

        if (show == "temp")
        {
            bans = bans.FindAll(x => !TimeUtility.IsPermanent(x.TimeUntil));
        }
        else if (show == "perma")
        {
            bans = bans.FindAll(x => TimeUtility.IsPermanent(x.TimeUntil));
        }

        if (bans.Count == 0)
        {
            ctx.Reply("No players in this ban list.");
            return;
        }

        bans.Sort((x, y) => y.Issued.CompareTo(x.Issued));

        string header = $"\n{banType} Bans:";

        List<string> messages = new() { header };
        int currentMessageIndex = 0;

        foreach (var user in bans)
        {
            string banInfo = Database.Messages[2].ToString(user, null);

            if (messages[currentMessageIndex].Length + banInfo.Length > MESSAGE_LIMIT)
            {
                currentMessageIndex++;
                messages.Add(banInfo);
            }
            else
            {
                messages[currentMessageIndex] += banInfo;
            }
        }

        int totalPages = (int)Math.Ceiling((double)messages.Count / MESSAGES_PER_PAGE);

        // always default to page 1 if requested page is invalid
        if (page < 1 || page > totalPages) page = 1;

        int startIndex = (page - 1) * MESSAGES_PER_PAGE;
        int endIndex = Math.Min(startIndex + MESSAGES_PER_PAGE, messages.Count);

        if (totalPages > 1) ctx.Reply($"Page {page} of {totalPages}:");
        for (int i = startIndex; i < endIndex; i++)
        {
            ctx.Reply(messages[i]);
        }
    }

    [Command(name: "loaduntracked", adminOnly: true)]
    public static void Backlog(ChatCommandContext ctx)
    {
        if (Database.SQL == null)
        {
            ctx.Reply("MySQL is not configured. Please configure MySQL using CrimsonSQL.");
            return;
        }

        if (!Settings.UseSQL.Value)
        {
            ctx.Reply("You are not using CrimsonSQL. Ensure it is installed and the setting is set to true.");
            return;
        }

        if (!SQLlink.Connect())
        {
            ctx.Reply("Connection could not be established with the SQL database.");
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
                ban.DatabaseId = -1;
                Database.AddBan(ban, Database.Banned);
            }

            ctx.Reply("Untracked server bans in banlist.txt imported and added to CrimsonBanned tracking.");
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

        if (!Settings.UseSQL.Value)
        {
            ctx.Reply("You are not using CrimsonSQL. Ensure it is installed and the setting is set to true.");
            return;
        }

        if (!SQLlink.Connect())
        {
            ctx.Reply("Connection could not be established with the SQL database.");
            return;
        }

        Database.SyncDB();

        ctx.Reply("Bans have been synced with SQL Database.");
    }

    [Command(name: "checkid", adminOnly: true)]
    public static void CheckID(ChatCommandContext ctx, string id)
    {
        ulong ID = Convert.ToUInt64(id);
        List<(Ban, BanDetails)> playerBans = new List<(Ban, BanDetails)>();

        List<Ban> allBans = new List<Ban>();

        if (Database.Banned?.Find(x => x.PlayerID == ID) is Ban serverBan)
            allBans.Add(serverBan);

        if (Database.ChatBans?.Find(x => x.PlayerID == ID) is Ban chatBan)
            allBans.Add(chatBan);

        if (Database.VoiceBans?.Find(x => x.PlayerID == ID) is Ban voiceBan)
            allBans.Add(voiceBan);

        if(allBans.Count == 0)
        {
            ctx.Reply($"No bans found for ID {ID}.");
            return;
        }

        bool ContainsNonLocal = false;
        foreach (var ban in allBans)
        {
            if (ban != null)
            {
                if (!ban.LocalBan) ContainsNonLocal = true;
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

        StringBuilder banList = new StringBuilder();
        banList.AppendLine(Database.Messages[0].ToString(playerBans[0].Item1, playerBans[0].Item2));
        if (Extensions.TryGetPlayerInfo(ID, out PlayerInfo playerInfo) && ContainsNonLocal)
        {
            banList.AppendLine($"Local server name: {playerInfo.User.CharacterName.ToString()}");
        }

        foreach (var ban in playerBans)
        {
            banList.AppendLine(Database.Messages[1].ToString(ban.Item1, ban.Item2));
        }

        ctx.Reply(banList.ToString());
    }

    [Command(name: "check", adminOnly: true)]
    public static void Check(ChatCommandContext ctx, string name)
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}.");
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
        banList.AppendLine(Database.Messages[0].ToString(playerBans[0].Item1, playerBans[0].Item2));
        foreach (var ban in playerBans)
        {
            banList.AppendLine(Database.Messages[1].ToString(ban.Item1, ban.Item2));
        }

        ctx.Reply(banList.ToString());
    }

    internal static bool GetBanDetails(Ban ban, List<Ban> list, out BanDetails details)
    {
        if (DateTime.Now > ban.TimeUntil.ToLocalTime())
        {
            details = null;
            Database.DeleteBan(ban, list);
            return false;
        }

        details = new BanDetails();
        details.Ban = ban;
        details.IssuedOn = ban.Issued.ToLocalTime().ToString("MM/dd/yy HH:mm");

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

        if (TimeUtility.IsPermanent(ban.TimeUntil))
        {
            details.RemainingTime = "Permanent";
        }
        else
        {
            details.RemainingTime = $"Expires in {ban.TimeUntil.ToLocalTime() - DateTime.Now}";
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
