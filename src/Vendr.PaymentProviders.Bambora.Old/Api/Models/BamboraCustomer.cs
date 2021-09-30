using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraCustomer
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
