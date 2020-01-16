using System;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.Bambora
{
    [PaymentProvider("bambora", "Bambora", "Basic payment provider for payments that will be processed via an external bambora system", Icon = "icon-invoice")]
    public class BamboraPaymentProvider : PaymentProviderBase<BamboraSettings>
    {
        public BamboraPaymentProvider(VendrContext vendr)
            : base(vendr)
        { }

        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool FinalizeAtContinueUrl => true;

        public override PaymentForm GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, BamboraSettings settings)
        {
            return new PaymentForm(continueUrl, FormMethod.Post);
        }

        public override string GetCancelUrl(OrderReadOnly order, BamboraSettings settings)
        {
            return string.Empty;
        }

        public override string GetErrorUrl(OrderReadOnly order, BamboraSettings settings)
        {
            return string.Empty;
        }

        public override string GetContinueUrl(OrderReadOnly order, BamboraSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ContinueUrl.MustNotBeNull("settings.ContinueUrl");

            return settings.ContinueUrl;
        }

        public override CallbackResponse ProcessCallback(OrderReadOnly order, HttpRequestBase request, BamboraSettings settings)
        {
            return new CallbackResponse
            {
                TransactionInfo = new TransactionInfo
                {
                    AmountAuthorized = order.TotalPrice.Value.WithTax,
                    TransactionFee = 0m,
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PaymentStatus = PaymentStatus.Authorized
                }
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
    }
}
