using RestSharp;
using RestSharp.Deserializers;

using Newtonsoft.Json;

namespace TwitchBot.Json
{
    class CustomJsonDeserializer : IDeserializer
    {
        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }

        /// <summary>
        /// Cusatom deserializer to handle null values.
        /// </summary>
        public type Deserialize<type>(IRestResponse response)
        {
            return JsonConvert.DeserializeObject<type>(response.Content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
