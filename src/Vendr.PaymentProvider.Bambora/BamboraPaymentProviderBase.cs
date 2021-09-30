using System;
using System.Text;
using System.Text.RegularExpressions;
using Vendr.Core.Models;
using Vendr.Core.Api;
using Vendr.Core.PaymentProviders;
using Vendr.PaymentProviders.Bambora.Api.Models;
using Vendr.Extensions;

namespace Vendr.PaymentProviders.Bambora
{
    public abstract class BamboraPaymentProviderBase<TSettings> : PaymentProviderBase<TSettings>
        where TSettings : BamboraSettingsBase, new()
    {
        public BamboraPaymentProviderBase(VendrContext vendr)
            : base(vendr)
        { }

        public override string GetCancelUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("settings");
            ctx.Settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            return ctx.Settings.CancelUrl;
        }

        public override string GetContinueUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("settings");
            ctx.Settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return ctx.Settings.ContinueUrl;
        }

        public override string GetErrorUrl(PaymentProviderContext<TSettings> ctx)
        {
            ctx.Settings.MustNotBeNull("settings");
            ctx.Settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return ctx.Settings.ErrorUrl;
        }

        protected BamboraClientConfig GetBamboraClientConfig(BamboraSettingsBase settings)
        {
            BamboraClientConfig config;

            if (settings.TestMode)
            {
                config = new BamboraClientConfig
                {
                    AccessKey = settings.TestAccessKey,
                    MerchantNumber = settings.TestMerchantNumber,
                    SecretKey = settings.TestSecretKey,
                    MD5Key = settings.TestMd5Key
                };
            }
            else
            {
                config = new BamboraClientConfig
                {
                    AccessKey = settings.LiveAccessKey,
                    MerchantNumber = settings.LiveMerchantNumber,
                    SecretKey = settings.LiveSecretKey,
                    MD5Key = settings.LiveMd5Key
                };
            }

            var apiKey = GenerateApiKey(config.AccessKey, config.MerchantNumber, config.SecretKey);

            config.Authorization = "Basic " + apiKey;

            return config;
        }

        protected PaymentStatus GetPaymentStatus(BamboraTransaction transaction)
        {
            if (transaction.Total.Credited > 0)
                return PaymentStatus.Refunded;

            if (transaction.Total.Declined > 0)
                return PaymentStatus.Cancelled;

            if (transaction.Total.Captured > 0)
                return PaymentStatus.Captured;

            if (transaction.Total.Authorized > 0)
                return PaymentStatus.Authorized;

            return PaymentStatus.Initialized;
        }

        protected string BamboraSafeOrderId(string orderId)
        {
            return Regex.Replace(orderId, "[^a-zA-Z0-9]", "");
        }

        private string GenerateApiKey(string accessToken, string merchantNumber, string secretToken)
        {
            var unencodedApiKey = $"{accessToken}@{merchantNumber}:{secretToken}";
            var unencodedApiKeyAsBytes = Encoding.UTF8.GetBytes(unencodedApiKey);
            return Convert.ToBase64String(unencodedApiKeyAsBytes);
        }
    }
}
