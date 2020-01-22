using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
