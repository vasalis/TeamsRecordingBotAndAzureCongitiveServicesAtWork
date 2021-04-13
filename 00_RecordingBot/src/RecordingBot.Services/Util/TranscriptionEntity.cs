using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordingBot.Services.Util
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// hey.
    /// </summary>
    public class TranscriptionEntity
    {
        /// <summary>
        /// Gets or sets id.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets call id.
        /// </summary>
        [JsonProperty(PropertyName = "callid")]
        public string CallId { get; set; }

        /// <summary>
        /// Gets or sets when.
        /// </summary>
        [JsonProperty(PropertyName = "when")]
        public DateTime When { get; set; }

        /// <summary>
        /// Gets or sets who.
        /// </summary>
        [JsonProperty(PropertyName = "who")]
        public string Who { get; set; }

        /// <summary>
        /// Gets or sets who id.
        /// </summary>
        [JsonProperty(PropertyName = "whoid")]
        public string WhoId { get; set; }

        /// <summary>
        /// Gets or sets text.
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets Translations.
        /// </summary>
        [JsonProperty(PropertyName = "translations")]
        public string Translations { get; set; }
    }
}
