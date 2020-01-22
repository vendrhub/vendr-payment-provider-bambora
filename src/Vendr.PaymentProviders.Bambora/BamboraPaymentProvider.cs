using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        // We'll finalize via webhook callback
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

            if (checkoutSession.Meta.Result) 
                return new PaymentForm(checkoutSession.Url, FormMethod.Get);

            throw new ApplicationException(checkoutSession.Meta.Message.EndUser);
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
                        // Fetch the transaction details so that we can work out
                        // the status of the transaction as the querystring params
                        // are not enough on their own
                        var transactionResp = client.GetTransaction(txnId);
                        if (transactionResp.Meta.Result)
                        {
                            return CallbackResponse.Ok(new TransactionInfo
                            {
                                TransactionId = transactionResp.Transaction.Id,
                                AmountAuthorized = AmountFromMinorUnits(amount + txnFee),
                                TransactionFee = AmountFromMinorUnits(txnFee),
                                PaymentStatus = GetPaymentStatus(transactionResp.Transaction)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Bambora - ProcessCallback");
            }

            return CallbackResponse.BadRequest();
        }

        public override ApiResponse FetchPaymentStatus(OrderReadOnly order, BamboraSettings settings)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.GetTransaction(order.TransactionInfo.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    return new ApiResponse(transactionResp.Transaction.Id, GetPaymentStatus(transactionResp.Transaction));
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Bambora - FetchPaymentStatus");
            }

            return null;
        }

        public override ApiResponse CancelPayment(OrderReadOnly order, BamboraSettings settings)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.DeleteTransaction(order.TransactionInfo.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    return new ApiResponse(order.TransactionInfo.TransactionId, PaymentStatus.Cancelled);
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Bambora - CancelPayment");
            }

            return null;
        }

        public override ApiResponse CapturePayment(OrderReadOnly order, BamboraSettings settings)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.CaptureTransaction(order.TransactionInfo.TransactionId, new BamboraAmountRequest
                {
                    Amount = AmountInMinorUnits(order.TransactionInfo.AmountAuthorized.Value)
                });

                if (transactionResp.Meta.Result)
                {
                    return new ApiResponse(order.TransactionInfo.TransactionId, PaymentStatus.Captured);
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Bambora - CapturePayment");
            }

            return null;
        }

        public override ApiResponse RefundPayment(OrderReadOnly order, BamboraSettings settings)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = client.CreditTransaction(order.TransactionInfo.TransactionId, new BamboraAmountRequest
                {
                    Amount = AmountInMinorUnits(order.TransactionInfo.AmountAuthorized.Value)
                });

                if (transactionResp.Meta.Result)
                {
                    return new ApiResponse(order.TransactionInfo.TransactionId, PaymentStatus.Refunded);
                }
            }
            catch (Exception ex)
            {
                Vendr.Log.Error<BamboraPaymentProvider>(ex, "Bambora - RefundPayment");
            }

            return null;
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
