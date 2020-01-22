using Flurl.Http;
using System.Text;
using System.Linq;
using System.Web;
using Vendr.PaymentProviders.Bambora.Api.Models;
using System;
using System.Security.Cryptography;

namespace Vendr.PaymentProviders.Bambora.Api
{
    public class BamboraClient
    {
        private BamboraClientConfig _config;

        public BamboraClient(BamboraClientConfig config)
        {
            _config = config;
        }

        public BamboraCheckoutSessionResponse CreateCheckoutSession(BamboraCreateCheckoutSessionRequest request)
        {
            var result = new FlurlRequest("https://api.v1.checkout.bambora.com/sessions")
                .AllowAnyHttpStatus()
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .PostJsonAsync(request)
                .ReceiveJson<BamboraCheckoutSessionResponse>()
                .Result;

            return result;
        }

        public BamboraTransactionResponse GetTransaction(string txnId)
        {
            var result = new FlurlRequest($"https://merchant-v1.api-eu.bambora.com/transactions/{txnId}")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .AllowAnyHttpStatus()
                .GetAsync()
                .ReceiveJson<BamboraTransactionResponse>()
                .Result;

            return result;
        }

        public BamboraResponse CaptureTransaction(string txnId, BamboraAmountRequest req = null)
        {
            var result = new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/capture")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .AllowAnyHttpStatus()
                .PostJsonAsync(req)
                .ReceiveJson<BamboraResponse>()
                .Result;

            return result;
        }

        public BamboraResponse CreditTransaction(string txnId, BamboraAmountRequest req)
        {
            var result = new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/credit")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .AllowAnyHttpStatus()
                .PostJsonAsync(req)
                .ReceiveJson<BamboraResponse>()
                .Result;

            return result;
        }

        public BamboraResponse DeleteTransaction(string txnId)
        {
            var result = new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/delete")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", "Basic " + GenerateApiKey(_config))
                .AllowAnyHttpStatus()
                .PostJsonAsync(null)
                .ReceiveJson<BamboraResponse>()
                .Result;

            return result;
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
