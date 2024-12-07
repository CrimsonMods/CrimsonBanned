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
        TimeUntil = timeUntil;
        Reason = reason;
        Issued = DateTime.Now.ToUniversalTime();
        IssuedBy = issuedBy;
    }
}

public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}