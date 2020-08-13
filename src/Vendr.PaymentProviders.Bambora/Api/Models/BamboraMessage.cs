using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraMessage
    {
        [JsonProperty("enduser")]
        public string EndUser { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }
    }
}
