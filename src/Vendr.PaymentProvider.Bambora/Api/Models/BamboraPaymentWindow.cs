using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraPaymentWindow
    {
        public static class Configurations
        {
            public const int Overlay = 1;
            public const int IFrame = 2;
            public const int FullScreen = 3;
            public const int Integrated = 4;

        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("paymentmethods")]
        public BamboraPaymentFilter[] PaymentMethods { get; set; }

        [JsonProperty("paymentgroups")]
        public BamboraPaymentFilter[] PaymentGroups { get; set; }

        [JsonProperty("paymenttypes")]
        public BamboraPaymentFilter[] PaymentTypes { get; set; }

        public BamboraPaymentWindow()
        {
            Id = Configurations.FullScreen;
        }
    }
}
