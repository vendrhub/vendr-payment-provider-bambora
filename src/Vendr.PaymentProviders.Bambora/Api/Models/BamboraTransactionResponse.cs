using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraTransactionResponse : BamboraResponse
    {
        [JsonProperty("transaction")]
        public BamboraTransaction Transaction { get; set; }
    }
}
