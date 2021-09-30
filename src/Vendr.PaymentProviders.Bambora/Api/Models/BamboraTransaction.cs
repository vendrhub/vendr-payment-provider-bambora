using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("orderid")]
        public string OrderId { get; set; }

        [JsonProperty("merchantnumber")]
        public string MerchantNumber { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("available")]
        public BamboraAvailableAmounts Available { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("total")]
        public BamboraTotals Total { get; set; }
    }
}
