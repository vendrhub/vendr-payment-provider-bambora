using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraCreateCheckoutSessionRequest
    {        
        [JsonProperty("instantcaptureamount")]
        public int InstantCaptureAmount { get; set; }

        [JsonProperty("order")]
        public BamboraOrder Order { get; set; }

        [JsonProperty("customer")]
        public BamboraCustomer Customer { get; set; }

        [JsonProperty("url")] // API is url (singular)
        public BamboraUrls Urls { get; set; }

        [JsonProperty("paymentwindow")]
        public BamboraPaymentWindow PaymentWindow { get; set; }
    }
}
