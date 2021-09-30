using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraOrder
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
