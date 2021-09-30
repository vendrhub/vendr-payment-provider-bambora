using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraPaymentFilter
    {
        public static class Actions
        {
            public const string Include = "include";
            public const string Exclude = "exclude";
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }
}
