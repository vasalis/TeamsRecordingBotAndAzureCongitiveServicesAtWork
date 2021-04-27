using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamsComModels
{
    public class CallEntity
    {        

        [JsonProperty(PropertyName = "callid")]
        public string CallId { get; set; }

        [JsonProperty(PropertyName = "when")]
        public DateTime When { get; set; }        

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }        
    }
}
