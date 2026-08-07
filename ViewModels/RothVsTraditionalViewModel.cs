namespace Reckoner.ViewModels
{
    public partial class RothVsTraditionalViewModel : BaseViewModel
    {
        public RothVsTraditionalViewModel(AppShellService appShell) : base(appShell) { }

        // Shared across both sections — same client, same timeline, same tax/growth assumptions.
        // All rate inputs are 0-100 for display — same convention as ContributionPercentageX100.
        [ObservableProperty] private decimal currentAge = 50m;
        [ObservableProperty] private decimal withdrawalAge = 65m;
        [ObservableProperty] private decimal currentTaxRatePercent = 24m;
        [ObservableProperty] private decimal futureTaxRatePercent = 22m;
        [ObservableProperty] private decimal growthRatePercent = 7m;
        [ObservableProperty] private decimal capitalGainsRatePercent = 15m;

        public int Years => (int)Math.Max(0, WithdrawalAge - CurrentAge);

        // --- Section 1: new contribution — Roth vs. Traditional ---
        [ObservableProperty] private decimal contributionAmount = 7000m;
        [ObservableProperty] private bool includeReinvestedSavings = false;

        public decimal TraditionalAfterTax => ContributionResult.TraditionalAfterTax;
        public decimal ReinvestedSavingsAfterTax => ContributionResult.ReinvestedSavingsAfterTax;
        public decimal TraditionalTotal => ContributionResult.TraditionalTotal;
        public decimal RothAfterTax => ContributionResult.RothAfterTax;
        public decimal Difference => ContributionResult.Difference;

        public string Summary
        {
            get
            {
                if (Years <= 0) return "Withdrawal age must be after current age.";
                var winner = Difference >= 0 ? "Roth" : "Traditional";
                var by = Math.Abs(Difference);
                return $"Over {Years} years, {winner} comes out ahead by {by:C0} after taxes"
                    + (IncludeReinvestedSavings ? " (including Traditional's reinvested tax savings)." : ".");
            }
        }

        private RothVsTraditionalResult ContributionResult => RothVsTraditionalCalculator.Calculate(new RothVsTraditionalInputs(
            ContributionAmount,
            Years,
            CurrentTaxRatePercent / 100m,
            FutureTaxRatePercent / 100m,
            GrowthRatePercent / 100m,
            IncludeReinvestedSavings,
            CapitalGainsRatePercent / 100m));

        // --- Section 2: existing balance — convert now vs. leave it ---
        [ObservableProperty] private decimal balanceToConvert = 100000m;
        [ObservableProperty] private bool payTaxFromOutsideCash = false;

        public decimal ConvertNowAfterTax => ConversionResult.ConvertNowAfterTax;
        public decimal DontConvertAfterTax => ConversionResult.DontConvertAfterTax;
        public decimal PreservedOutsideCashAfterTax => ConversionResult.PreservedOutsideCashAfterTax;
        public decimal DontConvertTotal => ConversionResult.DontConvertTotal;
        public decimal ConversionDifference => ConversionResult.Difference;

        public string ConversionSummary
        {
            get
            {
                if (Years <= 0) return "Withdrawal age must be after current age.";
                var winner = ConversionDifference >= 0 ? "Converting now" : "Not converting";
                var by = Math.Abs(ConversionDifference);
                return $"Over {Years} years, {winner} comes out ahead by {by:C0} after taxes"
                    + (PayTaxFromOutsideCash ? " (crediting the outside cash you'd have kept invested by not converting)." : ".");
            }
        }

        private ConversionResult ConversionResult => RothVsTraditionalCalculator.CalculateConversion(new ConversionInputs(
            BalanceToConvert,
            Years,
            CurrentTaxRatePercent / 100m,
            FutureTaxRatePercent / 100m,
            GrowthRatePercent / 100m,
            PayTaxFromOutsideCash,
            CapitalGainsRatePercent / 100m));

        // Shared inputs affect both sections; section-specific inputs affect just their own.
        partial void OnCurrentAgeChanged(decimal value) => RecomputeAll();
        partial void OnWithdrawalAgeChanged(decimal value) => RecomputeAll();
        partial void OnCurrentTaxRatePercentChanged(decimal value) => RecomputeAll();
        partial void OnFutureTaxRatePercentChanged(decimal value) => RecomputeAll();
        partial void OnGrowthRatePercentChanged(decimal value) => RecomputeAll();
        partial void OnCapitalGainsRatePercentChanged(decimal value) => RecomputeAll();

        partial void OnContributionAmountChanged(decimal value) => RecomputeContribution();
        partial void OnIncludeReinvestedSavingsChanged(bool value) => RecomputeContribution();

        partial void OnBalanceToConvertChanged(decimal value) => RecomputeConversion();
        partial void OnPayTaxFromOutsideCashChanged(bool value) => RecomputeConversion();

        private void RecomputeAll()
        {
            OnPropertyChanged(nameof(Years));
            RecomputeContribution();
            RecomputeConversion();
        }

        private void RecomputeContribution()
        {
            OnPropertyChanged(nameof(TraditionalAfterTax));
            OnPropertyChanged(nameof(ReinvestedSavingsAfterTax));
            OnPropertyChanged(nameof(TraditionalTotal));
            OnPropertyChanged(nameof(RothAfterTax));
            OnPropertyChanged(nameof(Difference));
            OnPropertyChanged(nameof(Summary));
        }

        private void RecomputeConversion()
        {
            OnPropertyChanged(nameof(ConvertNowAfterTax));
            OnPropertyChanged(nameof(DontConvertAfterTax));
            OnPropertyChanged(nameof(PreservedOutsideCashAfterTax));
            OnPropertyChanged(nameof(DontConvertTotal));
            OnPropertyChanged(nameof(ConversionDifference));
            OnPropertyChanged(nameof(ConversionSummary));
        }
    }
}
