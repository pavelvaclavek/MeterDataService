using System.Text.Json.Serialization;

namespace MeterDataService.Models
{
    public class MeterMessage
    {
        [JsonPropertyName("utc")]
        public string Utc { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public MeterResult Result { get; set; } = new();

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("n")]
        public string N { get; set; } = string.Empty;

        [JsonPropertyName("m")]
        public string M { get; set; } = string.Empty;

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

        public DateTime GetUtcDateTime()
        {
            if (long.TryParse(Utc, out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            }
            return DateTime.UtcNow;
        }
    }

    public class MeterResult
    {
        [JsonPropertyName("enc")]
        public string Enc { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }
}