using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BomboraResponse
    {
        [JsonProperty("meta")]
        public BomboraResponseMetaData Meta { get; set; }
    }
}
