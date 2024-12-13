using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrimsonBanned.Structs;

public class Ban
{
    public string PlayerName { get; set; }
    public ulong PlayerID { get; set; }
    [JsonConverter(typeof(JsonDateTimeConverter))]
    public DateTime TimeUntil { get; set; }
    public string Reason { get; set; }
    public string IssuedBy { get; set; }
    [JsonConverter(typeof(JsonDateTimeConverter))]
    public DateTime Issued { get; set; }
    public int DatabaseId { get; set; } = 0;
    public bool LocalBan { get; set; } = false;

    public Ban(string playerName, ulong playerID, DateTime timeUntil, string reason, string issuedBy)
    {
        PlayerName = playerName;
        PlayerID = playerID;
        TimeUntil = new DateTime(timeUntil.Year, timeUntil.Month, timeUntil.Day, timeUntil.Hour, timeUntil.Minute, timeUntil.Second, DateTimeKind.Utc);
        Reason = reason;
        DateTime now = DateTime.UtcNow;
        Issued = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        IssuedBy = issuedBy;
    }
}

public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var date = reader.GetDateTime();
        DateTime dateTime = new DateTime(date.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);
        
        // This is kind of non-sense. Our string format below is called ISO 8601 and is the default serialization format for DateTime
        // in Text.Json. However for some reason it isn't detecting that this is UTC? So we check if it knows it's UTC or not and then reaffirm that it is. 
        return dateTime.Kind == DateTimeKind.Utc
        ? dateTime
        : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}