using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraAmountRequest
    {
        [JsonProperty("amount")]
        public int Amount { get; set; }
    }
}
