using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraResponse
    {
        [JsonProperty("meta")]
        public BamboraResponseMetaData Meta { get; set; }
    }
}
