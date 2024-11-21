using System;
using System.Collections.Generic;

namespace CrimsonBanned.Structs;

public class Ban
{
    public string PlayerName { get; set; }
    public ulong PlayerID { get; set; }
    public DateTime TimeUntil { get; set; }
    public string Reason { get; set; }

    public Ban(string playerName, ulong playerID, DateTime timeUntil, string reason)
    {
        PlayerName = playerName;
        PlayerID = playerID;
        TimeUntil = timeUntil;
        Reason = reason;
    }
}

public class BansContainer
{
    public List<Ban> ChatBans { get; set; }
    public List<Ban> VoiceBans { get; set; }

    public BansContainer(List<Ban> _chats, List<Ban> _voices)
    {
        ChatBans = _chats;
        VoiceBans = _voices;
    }
}