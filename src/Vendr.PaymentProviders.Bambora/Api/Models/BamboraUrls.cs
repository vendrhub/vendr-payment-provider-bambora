using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api.Models
{
    public class BamboraUrls
    {
        [JsonProperty("immediateredirecttoaccept")]
        public int ImmediateRedirectToAccept { get; set; }

        [JsonProperty("accept")]
        public string Accept { get; set; }

        [JsonProperty("cancel")]
        public string Cancel { get; set; }

        [JsonProperty("callbacks")]
        public BamboraUrl[] Callbacks { get; set; }

        public BamboraUrls()
        {
            ImmediateRedirectToAccept = 1;
        }
    }
}
