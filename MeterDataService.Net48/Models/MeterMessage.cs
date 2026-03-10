using System;
using Newtonsoft.Json;

namespace MeterDataService.Net48.Models
{
    public class MeterMessage
    {
        [JsonProperty("utc")]
        public string Utc { get; set; } = string.Empty;

        [JsonProperty("result")]
        public MeterResult Result { get; set; } = new MeterResult();

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("n")]
        public string N { get; set; } = string.Empty;

        [JsonProperty("m")]
        public string M { get; set; } = string.Empty;

        [JsonProperty("network")]
        public string Network { get; set; } = string.Empty;

        [JsonProperty("system")]
        public string System { get; set; } = string.Empty;

        [JsonProperty("cname")]
        public string Cname { get; set; } = string.Empty;

        [JsonProperty("cdesc")]
        public string Cdesc { get; set; } = string.Empty;

        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("sn")]
        public string Sn { get; set; } = string.Empty;

        [JsonProperty("fid")]
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
        [JsonProperty("enc")]
        public string Enc { get; set; } = string.Empty;

        [JsonProperty("data")]
        public string Data { get; set; } = string.Empty;
    }
}
