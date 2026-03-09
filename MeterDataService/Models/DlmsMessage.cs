using System.Text.Json.Serialization;

namespace MeterDataService.Models;

public class DlmsMessage
{
    [JsonPropertyName("utc")]
    public long Utc { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("m")]
    public string M { get; set; } = string.Empty;

    [JsonPropertyName("n")]
    public int N { get; set; }

    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    [JsonPropertyName("system")]
    public string System { get; set; } = string.Empty;

    [JsonPropertyName("cname")]
    public string Cname { get; set; } = string.Empty;

    [JsonPropertyName("cdesc")]
    public string Cdesc { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("sn")]
    public string Sn { get; set; } = string.Empty;

    [JsonPropertyName("fid")]
    public string Fid { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public DlmsResult Result { get; set; } = new();
}

public class DlmsResult
{
    [JsonPropertyName("enc")]
    public string Enc { get; set; } = string.Empty;

    [JsonPropertyName("form")]
    public string Form { get; set; } = string.Empty;

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<DlmsDataEntry> Data { get; set; } = new();
}

public class DlmsDataEntry
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("attr")]
    public string Attr { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
