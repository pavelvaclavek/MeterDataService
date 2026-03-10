using System.Collections.Generic;
using Newtonsoft.Json;

namespace MeterDataService.Net48.Models
{
    public class DlmsMessage
    {
        [JsonProperty("utc")]
        public long Utc { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("m")]
        public string M { get; set; } = string.Empty;

        [JsonProperty("n")]
        public int N { get; set; }

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

        [JsonProperty("result")]
        public DlmsResult Result { get; set; } = new DlmsResult();
    }

    public class DlmsResult
    {
        [JsonProperty("enc")]
        public string Enc { get; set; } = string.Empty;

        [JsonProperty("form")]
        public string Form { get; set; } = string.Empty;

        [JsonProperty("protocol")]
        public string Protocol { get; set; } = string.Empty;

        [JsonProperty("data")]
        public List<DlmsDataEntry> Data { get; set; } = new List<DlmsDataEntry>();
    }

    public class DlmsDataEntry
    {
        [JsonProperty("class")]
        public string Class { get; set; } = string.Empty;

        [JsonProperty("attr")]
        public string Attr { get; set; } = string.Empty;

        [JsonProperty("value")]
        public string Value { get; set; } = string.Empty;
    }
}
