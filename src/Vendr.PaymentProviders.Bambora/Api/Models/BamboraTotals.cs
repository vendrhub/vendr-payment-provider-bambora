using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraTotals
    {
        [JsonProperty("authorized")]
        public int Authorized { get; set; }

        [JsonProperty("balance")]
        public int Balance { get; set; }

        [JsonProperty("captured")]
        public int Captured { get; set; }

        [JsonProperty("credited")]
        public int Credited { get; set; }

        [JsonProperty("declined")]
        public int Declined { get; set; }

        [JsonProperty("feeamount")]
        public int FedAmount { get; set; }
    }
}
