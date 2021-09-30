using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraAvailableAmounts
    {
        [JsonProperty("capture")]
        public int Capture { get; set; }

        [JsonProperty("credit")]
        public int Credit { get; set; }
    }
}
