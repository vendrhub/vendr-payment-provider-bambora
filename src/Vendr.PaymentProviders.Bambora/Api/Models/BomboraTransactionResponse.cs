using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BomboraTransactionResponse : BomboraResponse
    {
        [JsonProperty("transaction")]
        public BomboraTransaction Transaction { get; set; }
    }
}
