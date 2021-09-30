using System;
using System.Linq;
using Vendr.Core.Models;
using Vendr.Core.Api;
using Vendr.Core.PaymentProviders;
using Vendr.PaymentProviders.Bambora.Api;
using Vendr.PaymentProviders.Bambora.Api.Models;
using System.Threading.Tasks;
using Vendr.Common.Logging;

namespace Vendr.PaymentProviders.Bambora
{
    // https://developer.bambora.com/europe/checkout/getting-started/checkout-ctx.Settings#filter-payment-methods

    [PaymentProvider("bambora-checkout", "Bambora", "Bambora (formally ePay) payment provider for one time payments")]
    public class BamboraCheckoutPaymentProvider : BamboraPaymentProviderBase<BamboraCheckoutSettings>
    {
        private readonly ILogger<BamboraCheckoutPaymentProvider> _logger;

        public BamboraCheckoutPaymentProvider(VendrContext vendr,
            ILogger<BamboraCheckoutPaymentProvider> logger)
            : base(vendr)
        {
            _logger = logger;
        }

        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;
        public override bool CanFetchPaymentStatus => true;

        // We'll finalize via webhook callback
        public override bool FinalizeAtContinueUrl => false;

        public override async Task<PaymentFormResult> GenerateFormAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            var currency = Vendr.Services.CurrencyService.GetCurrency(ctx.Order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            // Ensure currency has valid ISO 4217 code
            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
            {
                throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
            }

            var orderAmount = (int)AmountToMinorUnits(ctx.Order.TransactionAmount.Value);

            var clientConfig = GetBamboraClientConfig(ctx.Settings);
            var client = new BamboraClient(clientConfig);

            // Configure checkout session
            var checkoutSessionRequest = new BamboraCreateCheckoutSessionRequest
            {
                InstantCaptureAmount = ctx.Settings.Capture ? orderAmount : 0,
                Customer = new BamboraCustomer
                {
                    Email = ctx.Order.CustomerInfo.Email
                },
                Order = new BamboraOrder
                {
                    Id = BamboraSafeOrderId(ctx.Order.OrderNumber),
                    Amount = orderAmount,
                    Currency = currencyCode
                },
                Urls = new BamboraUrls
                {
                    Accept = ctx.Urls.ContinueUrl,
                    Cancel = ctx.Urls.CancelUrl,
                    Callbacks = new[] {
                        new BamboraUrl { Url = ctx.Urls.CallbackUrl }
                    }
                },
                PaymentWindow = new BamboraPaymentWindow
                {
                    Id = 1,
                    Language = ctx.Settings.Language
                }
            };

            // Exclude payment methods
            if (!string.IsNullOrWhiteSpace(ctx.Settings.ExcludedPaymentMethods))
            {
                checkoutSessionRequest.PaymentWindow.PaymentMethods = ctx.Settings.ExcludedPaymentMethods
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment groups
            if (!string.IsNullOrWhiteSpace(ctx.Settings.ExcludedPaymentGroups))
            {
                checkoutSessionRequest.PaymentWindow.PaymentGroups = ctx.Settings.ExcludedPaymentGroups
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            // Exclude payment types
            if (!string.IsNullOrWhiteSpace(ctx.Settings.ExcludedPaymentTypes))
            {
                checkoutSessionRequest.PaymentWindow.PaymentTypes = ctx.Settings.ExcludedPaymentTypes
                    .Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => new BamboraPaymentFilter { Id = x.Trim(), Action = BamboraPaymentFilter.Actions.Exclude })
                    .ToArray();
            }

            var checkoutSession = await client.CreateCheckoutSessionAsync(checkoutSessionRequest);

            if (checkoutSession.Meta.Result)
            {
                return new PaymentFormResult()
                {
                    Form = new PaymentForm(checkoutSession.Url, PaymentFormMethod.Get)
                };
            }

            throw new ApplicationException(checkoutSession.Meta.Message.EndUser);
        }

        public override async Task<CallbackResult> ProcessCallbackAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(ctx.Settings);
                var client = new BamboraClient(clientConfig);

                if (client.ValidateRequest(ctx.Request, out var qs))
                {
                    var txnId = qs["txnid"];
                    var orderId = qs["orderid"];
                    var amount = int.Parse("0" + qs["amount"]);
                    var txnFee = int.Parse("0" + qs["txnfee"]);

                    // Validate params
                    if (!string.IsNullOrWhiteSpace(txnId)
                        && !string.IsNullOrWhiteSpace(orderId)
                        && orderId == BamboraSafeOrderId(ctx.Order.OrderNumber)
                        && amount > 0)
                    {
                        // Fetch the transaction details so that we can work out
                        // the status of the transaction as the querystring params
                        // are not enough on their own
                        var transactionResp = await client.GetTransactionAsync(txnId);
                        if (transactionResp.Meta.Result)
                        {
                            return CallbackResult.Ok(new TransactionInfo
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
                _logger.Error(ex, "Bambora - ProcessCallback");
            }

            return CallbackResult.BadRequest();
        }

        public override async Task<ApiResult> FetchPaymentStatusAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(ctx.Settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = await client.GetTransactionAsync(ctx.Order.TransactionInfo.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate()
                        {
                            TransactionId = transactionResp.Transaction.Id,
                            PaymentStatus = GetPaymentStatus(transactionResp.Transaction)
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Bambora - FetchPaymentStatus");
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> CancelPaymentAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(ctx.Settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = await client.DeleteTransactionAsync(ctx.Order.TransactionInfo.TransactionId);
                if (transactionResp.Meta.Result)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate()
                        {
                            TransactionId = ctx.Order.TransactionInfo.TransactionId,
                            PaymentStatus = PaymentStatus.Cancelled
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Bambora - CancelPayment");
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> CapturePaymentAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(ctx.Settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = await client.CaptureTransactionAsync(ctx.Order.TransactionInfo.TransactionId, new BamboraAmountRequest
                {
                    Amount = (int)AmountToMinorUnits(ctx.Order.TransactionInfo.AmountAuthorized.Value)
                });

                if (transactionResp.Meta.Result)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate()
                        {
                            TransactionId = ctx.Order.TransactionInfo.TransactionId,
                            PaymentStatus = PaymentStatus.Captured
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Bambora - CapturePayment");
            }

            return ApiResult.Empty;
        }

        public override async Task<ApiResult> RefundPaymentAsync(PaymentProviderContext<BamboraCheckoutSettings> ctx)
        {
            try
            {
                var clientConfig = GetBamboraClientConfig(ctx.Settings);
                var client = new BamboraClient(clientConfig);

                var transactionResp = await client.CreditTransactionAsync(ctx.Order.TransactionInfo.TransactionId, new BamboraAmountRequest
                {
                    Amount = (int)AmountToMinorUnits(ctx.Order.TransactionInfo.AmountAuthorized.Value)
                });

                if (transactionResp.Meta.Result)
                {
                    return new ApiResult
                    {
                        TransactionInfo = new TransactionInfoUpdate()
                        {
                            TransactionId = ctx.Order.TransactionInfo.TransactionId,
                            PaymentStatus = PaymentStatus.Refunded
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Bambora - RefundPayment");
            }

            return ApiResult.Empty;
        }
    }
}
