using Flurl.Http;
using System.Text;
using System.Linq;
using System.Web;
using Vendr.PaymentProviders.Bambora.Api.Models;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Specialized;

namespace Vendr.PaymentProviders.Bambora.Api
{
    public class BamboraClient
    {
        private BamboraClientConfig _config;

        public BamboraClient(BamboraClientConfig config)
        {
            _config = config;
        }

        public async Task<BamboraCheckoutSessionResponse> CreateCheckoutSessionAsync(BamboraCreateCheckoutSessionRequest request)
        {
            return await new FlurlRequest("https://api.v1.checkout.bambora.com/sessions")
                .AllowAnyHttpStatus()
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", _config.Authorization)
                .PostJsonAsync(request)
                .ReceiveJson<BamboraCheckoutSessionResponse>();
        }

        public async Task<BamboraTransactionResponse> GetTransactionAsync(string txnId)
        {
            return await new FlurlRequest($"https://merchant-v1.api-eu.bambora.com/transactions/{txnId}")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", _config.Authorization)
                .AllowAnyHttpStatus()
                .GetAsync()
                .ReceiveJson<BamboraTransactionResponse>();
        }

        public async Task<BamboraResponse> CaptureTransactionAsync(string txnId, BamboraAmountRequest req = null)
        {
            return await new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/capture")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", _config.Authorization)
                .AllowAnyHttpStatus()
                .PostJsonAsync(req)
                .ReceiveJson<BamboraResponse>();
        }

        public async Task<BamboraResponse> CreditTransactionAsync(string txnId, BamboraAmountRequest req)
        {
            return await new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/credit")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", _config.Authorization)
                .AllowAnyHttpStatus()
                .PostJsonAsync(req)
                .ReceiveJson<BamboraResponse>();
        }

        public async Task<BamboraResponse> DeleteTransactionAsync(string txnId)
        {
            return await new FlurlRequest($"https://transaction-v1.api-eu.bambora.com/transactions/{txnId}/delete")
                .WithHeader("Accept", "application/json")
                .WithHeader("Authorization", _config.Authorization)
                .AllowAnyHttpStatus()
                .PostJsonAsync(null)
                .ReceiveJson<BamboraResponse>();
        }

        public bool ValidateRequest(HttpRequestMessage request, out NameValueCollection queryString)
        {
            queryString = HttpUtility.ParseQueryString(request.RequestUri.Query);

            var hash = queryString["hash"];

            if (string.IsNullOrWhiteSpace(hash))
                return false;

            var toHash = new StringBuilder();

            foreach (var key in queryString.AllKeys.Where(x => !x.Equals("hash", StringComparison.InvariantCultureIgnoreCase)))
            {
                toHash.Append(queryString[key]);
            }

            toHash.Append(_config.MD5Key);

            var calculatedHash = GetMD5Hash(toHash.ToString());

            return hash == calculatedHash;
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
