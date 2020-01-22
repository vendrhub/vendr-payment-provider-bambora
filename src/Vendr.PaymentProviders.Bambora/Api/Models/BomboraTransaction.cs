using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BomboraTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
