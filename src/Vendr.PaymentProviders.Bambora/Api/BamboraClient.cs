using Flurl.Http;
using System.Text;
using System.Linq;
using System.Web;
using Vendr.PaymentProviders.Bambora.Api.Models;
using System;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Vendr.PaymentProviders.Bambora.Api
{
    public class BamboraClient
    {
        private BamboraClientConfig _config;

        public BamboraClient(BamboraClientConfig config)
        {
            _config = config;
        }

        public BamboraCheckoutSession CreateCheckoutSession(BamboraCreateCheckoutSessionRequest request)
        {
            var apiKey = GenerateApiKey(_config);

            return new FlurlRequest("https://api.v1.checkout.bambora.com/sessions")
                .WithHeader("Content-Type", "application/json")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", "Basic " + apiKey)
                .PostJsonAsync(request)
                .ReceiveJson<BamboraCheckoutSession>()
                .Result;
        }

        public BomboraTransaction GetTransaction(string txnId)
        {
            var result = new FlurlRequest($"https://merchant-v1.api-eu.bambora.com/transactions/{txnId}")
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .GetAsync()
                .ReceiveJson<BomboraTransactionResponse>()
                .Result;

            // TODO: Log unsuccessful request

            return result.Meta.Result
                ? result.Transaction
                : null;
        }

        public bool ValidateRequest(HttpRequestBase request)
        {
            var hash = request.QueryString["hash"];

            if (string.IsNullOrWhiteSpace(hash))
                return false;

            var toHash = new StringBuilder();

            foreach (var key in request.QueryString.AllKeys.Where(x => !x.Equals("hash", StringComparison.InvariantCultureIgnoreCase)))
            {
                toHash.Append(request.QueryString[key]);
            }

            toHash.Append(_config.MD5Key);

            var calculatedHash = GetMD5Hash(toHash.ToString());

            return hash == calculatedHash;
        }

        private string GenerateApiKey(BamboraClientConfig config)
        {
            return GenerateApiKey(config.AccessKey, config.MerchantNumber, config.SecretKey);
        }

        private string GenerateApiKey(string accessToken, string merchantNumber, string secretToken)
        {
            var unencodedApiKey = $"{accessToken}@{merchantNumber}:{secretToken}";
            var unencodedApiKeyAsBytes = Encoding.UTF8.GetBytes(unencodedApiKey);
            return Convert.ToBase64String(unencodedApiKeyAsBytes);
        }

        private string GetMD5Hash(string input)
        {
            var hash = new StringBuilder();

            using (var md5provider = new MD5CryptoServiceProvider())
            {
                var bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

                for (var i = 0; i < bytes.Length; i++)
                {
                    hash.Append(bytes[i].ToString("x2"));
                }
            }

            return hash.ToString();
        }
    }
}
