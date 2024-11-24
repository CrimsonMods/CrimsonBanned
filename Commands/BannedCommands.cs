
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CrimsonBanned.Structs;
using Unity.Collections;
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
            "chat" => Database.ChatBans,
            "voice" => Database.VoiceBans,
            _ => []
        };

        string s = "";

        List<string> messages = new() { s };
        int currentMessageIndex = 0;

        foreach (var user in bans)
        {
            s += $"{user.PlayerName} - {user.TimeUntil - DateTime.Now}\n";

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

                Ban ban = new Ban("unknown", Convert.ToUInt64(line), DateTime.MinValue, "Synced from local banlist.txt file.");
                Database.AddBan(ban, Database.Banned);
            }

            ctx.Reply("Local banlist.txt synced to MySQL database.");
        }
        else
        {
            ctx.Reply("Local banlist.txt not found. Please specify the file path.");
        }
    }

    [Command(name: "check", adminOnly: true)]
    public static void Check(ChatCommandContext ctx, string name)
    {
        if (!Extensions.TryGetPlayerInfo(name, out PlayerInfo playerInfo))
        {
            ctx.Reply($"Could not find player with the name {name}");
            return;
        }

        List<(string, string)> playerBans = new List<(string, string)>();

        if (Database.ChatBans.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            Ban ban = Database.ChatBans.Find(x => x.PlayerID == playerInfo.User.PlatformId);
            (string, string) m = AddBanMessage("Chat", ban, Database.ChatBans);
            if(m.Item1 != "Removed") playerBans.Add(m);
        }

        if (Database.VoiceBans.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            Ban ban = Database.VoiceBans.Find(x => x.PlayerID == playerInfo.User.PlatformId);
            (string, string) m = AddBanMessage("Voice", ban, Database.VoiceBans);
            if(m.Item1 != "Removed") playerBans.Add(m);
        }

        if (Database.Banned.Exists(x => x.PlayerID == playerInfo.User.PlatformId))
        {
            Ban ban = Database.Banned.Find(x => x.PlayerID == playerInfo.User.PlatformId);
            (string, string) m = AddBanMessage("Server", ban, Database.Banned);
            if(m.Item1 != "Removed") playerBans.Add(m);
        }

        if (playerBans.Count == 0)
        {
            ctx.Reply($"{name} is not banned.");
            return;
        }

        StringBuilder banList = new StringBuilder();
        banList.AppendLine($"\n{playerInfo.User.CharacterName.ToString()}'s Bans:");

        foreach (var ban in playerBans)
        {
            banList.AppendLine($"{ban.Item1} - {ban.Item2}");
        }

        ctx.Reply(banList.ToString());
    }

    static (string, string) AddBanMessage(string banType, Ban ban, List<Ban> banList)
    {
        (string, string) message = (banType, "");
        if (ban.TimeUntil == DateTime.MinValue)
        {
            message.Item2 = "Permanent";
        }
        else if (DateTime.Now > ban.TimeUntil)
        {
            Database.DeleteBan(ban, banList);
            return ("Removed", "");
        }
        else
        {
            message.Item2 = $"Expires in {ban.TimeUntil - DateTime.Now}";
        }
        return message;
    }
}
