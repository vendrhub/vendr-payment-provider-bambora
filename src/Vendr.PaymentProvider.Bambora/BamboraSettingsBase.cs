using Vendr.Core.PaymentProviders;

namespace Vendr.PaymentProviders.Bambora
{
    public class BamboraSettingsBase
    {
        [PaymentProviderSetting(Name = "Continue URL",
            Description = "The URL to continue to after this provider has done processing. eg: /continue/",
            SortOrder = 100)]
        public string ContinueUrl { get; set; }

        [PaymentProviderSetting(Name = "Cancel URL",
            Description = "The URL to return to if the payment attempt is canceled. eg: /cancel/",
            SortOrder = 200)]
        public string CancelUrl { get; set; }

        [PaymentProviderSetting(Name = "Error URL",
            Description = "The URL to return to if the payment attempt errors. eg: /error/",
            SortOrder = 300)]
        public string ErrorUrl { get; set; }

        [PaymentProviderSetting(Name = "Test Merchant Number",
            Description = "Your Bambora Merchant Number for test transactions.",
            SortOrder = 400)]
        public string TestMerchantNumber { get; set; }

        [PaymentProviderSetting(Name = "Live Merchant Number",
            Description = "Your Bambora Merchant Number for live transactions.",
            SortOrder = 500)]
        public string LiveMerchantNumber { get; set; }

        [PaymentProviderSetting(Name = "Test Access Key",
            Description = "The test API Access Key obtained from the Bambora portal.",
            SortOrder = 600)]
        public string TestAccessKey { get; set; }

        [PaymentProviderSetting(Name = "Live Access Key",
            Description = "The live API Access Key obtained from the Bambora portal.",
            SortOrder = 700)]
        public string LiveAccessKey { get; set; }

        [PaymentProviderSetting(Name = "Test Secret Key",
            Description = "The test API Secret Key obtained from the Bambora portal.",
            SortOrder = 800)]
        public string TestSecretKey { get; set; }

        [PaymentProviderSetting(Name = "Live Secret Key",
            Description = "The live API Secret Key obtained from the Bambora portal.",
            SortOrder = 900)]
        public string LiveSecretKey { get; set; }

        [PaymentProviderSetting(Name = "Test MD5 Key",
            Description = "The test MD5 hashing key obtained from the Bambora portal.",
            SortOrder = 1000)]
        public string TestMd5Key { get; set; }

        [PaymentProviderSetting(Name = "Live MD5 Key",
            Description = "The live MD5 hashing key obtained from the Bambora portal.",
            SortOrder = 1100)]
        public string LiveMd5Key { get; set; }

        [PaymentProviderSetting(Name = "Language",
            Description = "Set the language to use for the payment portal. Can be 'en-GB', 'da-DK', 'sv-SE' or 'nb-NO'.",
            SortOrder = 1200)]
        public string Language { get; set; }

        [PaymentProviderSetting(Name = "Capture",
            Description = "Flag indicating whether to immediately capture the payment, or whether to just authorize the payment for later (manual) capture.",
            SortOrder = 1300)]
        public bool Capture { get; set; }

        [PaymentProviderSetting(Name = "Test Mode",
            Description = "Set whether to process payments in test mode.",
            SortOrder = 10000)]
        public bool TestMode { get; set; }

        // Advanced settings

        [PaymentProviderSetting(Name = "Excluded Payment Methods",
            Description = "Comma separated list of Payment Method IDs to exclude.",
            SortOrder = 100,
            IsAdvanced = true)]
        public string ExcludedPaymentMethods { get; set; }

        [PaymentProviderSetting(Name = "Excluded Payment Groups",
            Description = "Comma separated list of Payment Group IDs to exclude.",
            SortOrder = 200,
            IsAdvanced = true)]
        public string ExcludedPaymentGroups { get; set; }

        [PaymentProviderSetting(Name = "Excluded Payment Types",
            Description = "Comma separated list of Payment Type IDs to exclude.",
            SortOrder = 300,
            IsAdvanced = true)]
        public string ExcludedPaymentTypes { get; set; }
    }
}
