using System;
using CrimsonBanned.Commands;
using CrimsonBanned.Structs;
using CrimsonBanned.Utilities;
using static CrimsonBanned.Services.PlayerService;

public struct MessagePair
{
    public string Key { get; set; }
    public string Value { get; set; }

    public MessagePair(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string ToString(Ban ban, BanDetails details = null)
    {
        string message = Value;
        if (string.IsNullOrEmpty(ban.PlayerName))
        {
            message = message.Replace("{player}", "<i>Unknown</i>");
        }
        else
        {
            message = message.Replace("{player}", ban.PlayerName);
        }
        
        message = message.Replace("{id}", ban.PlayerID.ToString());
        message = message.Replace("{issued}", ban.Issued.ToString("MM/dd/yy HH:mm"));
        message = message.Replace("{reason}", ban.Reason);
        message = message.Replace("{by}", ban.IssuedBy);
        message = message.Replace("{local}", ban.LocalBan.ToString());
        
        if(details != null)
        {
            message = message.Replace("{type}", details.BanType);
        }
        else
        {
            message = message.Replace("{type}", "");
        }

        if(IsPermanent(ban.TimeUntil))
        {
            message = message.Replace("{until}", "Permanent");
            message = message.Replace("{remainder}", "Permanent");
        }
        else
        {
            message = message.Replace("{until}", ban.TimeUntil.ToString("MM/dd/yy HH:mm"));
            message = message.Replace("{remainder}", TimeUtility.FormatRemainder(ban.TimeUntil));
        }

        return message;
    }

    private bool IsPermanent(DateTime date)
    {
        return date == DateTime.MinValue;
    }
}