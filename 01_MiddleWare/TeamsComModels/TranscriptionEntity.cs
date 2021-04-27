using Newtonsoft.Json;
using System;

namespace TeamsComModels
{
    public class TranscriptionEntity
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "callid")]
        public string CallId { get; set; }

        [JsonProperty(PropertyName = "when")]
        public DateTime When { get; set; }

        [JsonProperty(PropertyName = "who")]
        public string Who { get; set; }

        [JsonProperty(PropertyName = "whoid")]
        public string WhoId { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "translations")]
        public string Translations { get; set; }
    }
}
