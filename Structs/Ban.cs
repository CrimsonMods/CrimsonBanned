using System;
using System.Collections.Generic;

namespace CrimsonBanned.Structs;

public class Ban
{
    public string PlayerName { get; set; }
    public ulong PlayerID { get; set; }
    public DateTime TimeUntil { get; set; }
    public string Reason { get; set; }
    public string IssuedBy { get; set; }
    public DateTime Issued { get; set; }
    public int DatabaseId { get; set; } = 0;
    public bool LocalBan { get; set; } = false;

    public Ban(string playerName, ulong playerID, DateTime timeUntil, string reason, string issuedBy)
    {
        PlayerName = playerName;
        PlayerID = playerID;
        TimeUntil = timeUntil;
        Reason = reason;
        Issued = DateTime.Now.ToUniversalTime();
        IssuedBy = issuedBy;
    }
}