using CrimsonBanned.Structs;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrimsonBanned.Services;

internal class JSONBinService
{
    private static HttpClient HttpClient;

    public JSONBinService()
    {
        if (!Settings.JSONBinConfigured) return;
        if (HttpClient != null) return;

        HttpClient = new HttpClient();

        var masterKey = Settings.JSONBinAPIKey.Value.Trim().Replace(" ", "");
        HttpClient.DefaultRequestHeaders.Add("X-Master-Key", masterKey);
        HttpClient.DefaultRequestHeaders.Add("X-Bin-Versioning", "false");
        HttpClient.BaseAddress = new Uri("https://api.jsonbin.io/v3");

        Plugin.LogInstance.LogInfo("JSONBin API initialized.");
    }

    public static async Task<BansContainer> GetBans()
    {
        if (HttpClient == null) new JSONBinService();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/b/{Settings.JSONBinID.Value.Trim()}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var result = await HttpClient.SendAsync(request);
        if (result.IsSuccessStatusCode)
        {
            var content = await result.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JSONBinResponse>(content);
            return json.record;
        }

        Plugin.LogInstance.LogError($"Failed to get bans: {result.StatusCode} - {result.ReasonPhrase}");
        return null;
    }

    public static async Task<bool> UpdateBans(BansContainer container)
    {
        string json = JsonSerializer.Serialize(container, Database.prettyJsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await HttpClient.PutAsync($"b/{Settings.JSONBinID}", content);

        if (result.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            Plugin.LogInstance.LogError($"Failed to update bans: {result.StatusCode} - {result.ReasonPhrase}");
            return false;
        }
    }
}

public class JsonErrorResponse
{
    public string Message { get; set; }
}

public class JSONBinResponse
{
    public BansContainer record { get; set; }
    public JsonMetadata metadata { get; set; }
}

public class JsonMetadata
{
    public string id { get; set; }
    public bool @private { get; set; }
    public string createdAt { get; set; }
}
