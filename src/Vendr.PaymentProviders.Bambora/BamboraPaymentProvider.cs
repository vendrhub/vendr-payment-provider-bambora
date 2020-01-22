using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.Bambora.Api;
using Vendr.PaymentProviders.Bambora.Api.Models;

namespace Vendr.PaymentProviders.Bambora
{
    // https://developer.bambora.com/europe/checkout/getting-started/checkout-settings#filter-payment-methods

    [PaymentProvider("bambora", "Bambora", "Bambora (formally ePay) payment provider for one time payments")]
    public class BamboraPaymentProvider : PaymentProviderBase<BamboraSettings>
    {
        public BamboraPaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;
        public override bool CanFetchPaymentStatus => true;

        public override bool FinalizeAtContinueUrl => false;

        public override string GetCancelUrl(OrderReadOnly order, BamboraSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");

            return settings.CancelUrl;
        }

        public override string GetErrorUrl(OrderReadOnly order, BamboraSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");

            return settings.ErrorUrl;
        }

        public override string GetContinueUrl(OrderReadOnly order, BamboraSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override PaymentForm GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, BamboraSettings settings)
        {
            var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
            var orderAmount = AmountInMinorUnits(order.TotalPrice.Value.WithTax);

            var clientConfig = GetBamboraClientConfig(settings);
            var client = new BamboraClient(clientConfig);

            // Configure checkout session
            var checkoutSessionRequest = new BamboraCreateCheckoutSessionRequest
            {
                InstantCaptureAmount = settings.Capture ? orderAmount : 0,
                Customer = new BamboraCustomer
                {
                    Email = order.CustomerInfo.Email
                },
                Order = new BamboraOrder
                {
                    Id = BamboraSafeOrderId(order.OrderNumber),
                    Amount = orderAmount,
                    Currency = currency.Code
                },
                Urls = new BamboraUrls
                {
                    Accept = continueUrl,
                    Cancel = cancelUrl,
                    Callbacks = new[] {
                        new BamboraUrl { Url = callbackUrl }
                    }
                },
                PaymentWindow = new BamboraPaymentWindow
                {
                    Id = 1,
                    Language = settings.Language
                }
            };

            // Exclude payment methods
            if (!string.IsNullOrWhiteSpace(settings.ExcludedPaymentMethods))
            {
                checkoutSessionRequest.PaymentWindow.PaymentMethods = settings.ExcludedPaymentMethods
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment groups
            if (!string.IsNullOrWhiteSpace(settings.ExcludedPaymentGroups))
            {
                checkoutSessionRequest.PaymentWindow.PaymentGroups = settings.ExcludedPaymentGroups
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment types
            if (!string.IsNullOrWhiteSpace(settings.ExcludedPaymentTypes))
            {
                checkoutSessionRequest.PaymentWindow.PaymentTypes = settings.ExcludedPaymentTypes
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            var checkoutSession = client.CreateCheckoutSession(checkoutSessionRequest);

            return new PaymentForm(checkoutSession.Url, FormMethod.Get);
        }

        public override CallbackResponse ProcessCallback(OrderReadOnly order, HttpRequestBase request, BamboraSettings settings)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(settings);
                var client = new BamboraClient(clientConfig);

                if (client.ValidateRequest(request))
                {
                    var txnId = request.QueryString["txnid"];
                    var orderId = request.QueryString["orderid"];
                    var amount = int.Parse("0" + request.QueryString["amount"]);
                    var txnFee = int.Parse("0" + request.QueryString["txnfee"]);

                    // Validate params
                    if (!string.IsNullOrWhiteSpace(txnId)
                        && !string.IsNullOrWhiteSpace(orderId)
                        && orderId == BamboraSafeOrderId(order.OrderNumber)
                        && amount > 0)
                    {
                        // TODO: Could do with knowing a better way if the payment is captured
                        // as it's possible a provider could be updated after transaction is
                        // triggered but before callback is received
                        return new CallbackResponse
                        {
                            TransactionInfo = new TransactionInfo
                            {
                                TransactionId = txnId,
                                AmountAuthorized = AmountFromMinorUnits(amount + txnFee),
                                TransactionFee = AmountFromMinorUnits(txnFee),
                                PaymentStatus = settings.Capture ? PaymentStatus.Captured : PaymentStatus.Authorized
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Stripe - ProcessCallback");
            }

            return new CallbackResponse
            {
                HttpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            };
        }

        public override ApiResponse CancelPayment(OrderReadOnly order, BamboraSettings settings)
        {
            return new ApiResponse(order.TransactionInfo.TransactionId, PaymentStatus.Cancelled);
        }

        public override ApiResponse CapturePayment(OrderReadOnly order, BamboraSettings settings)
        {
            return new ApiResponse(order.TransactionInfo.TransactionId, PaymentStatus.Captured);
        }

        protected BamboraClientConfig GetBamboraClientConfig(BamboraSettings settings)
        {
            if (settings.Mode == BamboraMode.Test)
            {
                return new BamboraClientConfig
                {
                    AccessKey = settings.TestAccessKey,
                    MerchantNumber = settings.TestMerchantNumber,
                    SecretKey = settings.TestSecretKey,
                    MD5Key = settings.TestMd5Key
                };
            }
            else
            {
                return new BamboraClientConfig
                {
                    AccessKey = settings.LiveAccessKey,
                    MerchantNumber = settings.LiveMerchantNumber,
                    SecretKey = settings.LiveSecretKey,
                    MD5Key = settings.LiveMd5Key
                };
            }
        }

        protected string BamboraSafeOrderId(string orderId)
        {
            return Regex.Replace(orderId, "[^a-zA-Z0-9]", "");
        }

        protected static int AmountInMinorUnits(decimal val)
        {
            var cents = val * 100M;
            var centsRounded = Math.Round(cents, MidpointRounding.AwayFromZero);
            return Convert.ToInt32(centsRounded);
        }

        protected static decimal AmountFromMinorUnits(int val)
        {
            return val / 100M;
        }
    }
}
