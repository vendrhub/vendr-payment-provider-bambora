using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BomboraResponseMetaData
    {
        [JsonProperty("result")]
        public bool Result { get; set; }
    }
}
