namespace Reckoner.Services
{
    // All rate inputs are fractions (0.24, not 24) — the ViewModel handles the 0-100 display
    // conversion, same convention as SecurityHolding.ContributionPercentageX100.
    public record RothVsTraditionalInputs(
        decimal ContributionAmount,
        int Years,
        decimal CurrentTaxRate,
        decimal FutureTaxRate,
        decimal GrowthRate,
        bool IncludeReinvestedSavings,
        decimal CapitalGainsRate);

    public record RothVsTraditionalResult(
        decimal TraditionalAfterTax,
        decimal ReinvestedSavingsAfterTax,
        decimal RothAfterTax)
    {
        public decimal TraditionalTotal => TraditionalAfterTax + ReinvestedSavingsAfterTax;
        public decimal Difference => RothAfterTax - TraditionalTotal;
    }

    // Phase 2 — an existing Traditional balance: convert it now (pay tax today at CurrentTaxRate,
    // then it grows tax-free) vs. leave it (grows tax-deferred, taxed at FutureTaxRate whenever
    // withdrawn). PayTaxFromOutsideCash mirrors the contribution comparison's reinvestment
    // toggle, just mirrored: converting "for free" (from outside cash) only looks that good if
    // you also credit the Don't-Convert side with what that same cash would have grown to sitting
    // in a taxable account instead of being spent on the conversion's tax bill.
    public record ConversionInputs(
        decimal BalanceToConvert,
        int Years,
        decimal CurrentTaxRate,
        decimal FutureTaxRate,
        decimal GrowthRate,
        bool PayTaxFromOutsideCash,
        decimal CapitalGainsRate);

    public record ConversionResult(
        decimal ConvertNowAfterTax,
        decimal DontConvertAfterTax,
        decimal PreservedOutsideCashAfterTax)
    {
        public decimal DontConvertTotal => DontConvertAfterTax + PreservedOutsideCashAfterTax;
        public decimal Difference => ConvertNowAfterTax - DontConvertTotal;
    }

    // Compares contributing the same dollar amount to a Traditional vs. a Roth account — the
    // fair comparison given IRA/401(k) limits are a shared cap across both, not separate (see
    // NOTES.md-adjacent discussion: Traditional can't just "contribute more" for the same
    // take-home cost the way an uncapped taxable account could).
    //
    // Traditional grows tax-deferred and is taxed as ordinary income at FutureTaxRate on
    // withdrawal. Roth grows tax-free. When IncludeReinvestedSavings is on, Traditional's
    // this-year tax deduction (ContributionAmount * CurrentTaxRate) is modeled as a third leg:
    // that cash reinvested in a taxable account, taxed once as a long-term capital gain on the
    // total gain at withdrawal — without this, comparing "same contribution to either" ignores
    // that Traditional also freed up cash today.
    public static class RothVsTraditionalCalculator
    {
        public static RothVsTraditionalResult Calculate(RothVsTraditionalInputs inputs)
        {
            decimal growthFactor = Pow(1 + inputs.GrowthRate, inputs.Years);

            decimal traditionalGross = inputs.ContributionAmount * growthFactor;
            decimal traditionalAfterTax = traditionalGross * (1 - inputs.FutureTaxRate);

            decimal rothAfterTax = inputs.ContributionAmount * growthFactor;

            decimal reinvestedSavingsAfterTax = 0m;
            if (inputs.IncludeReinvestedSavings)
            {
                decimal taxSavings = inputs.ContributionAmount * inputs.CurrentTaxRate;
                decimal taxableGross = taxSavings * growthFactor;
                decimal taxableGain = taxableGross - taxSavings;
                reinvestedSavingsAfterTax = taxSavings + taxableGain * (1 - inputs.CapitalGainsRate);
            }

            return new RothVsTraditionalResult(traditionalAfterTax, reinvestedSavingsAfterTax, rothAfterTax);
        }

        public static ConversionResult CalculateConversion(ConversionInputs inputs)
        {
            decimal growthFactor = Pow(1 + inputs.GrowthRate, inputs.Years);
            decimal taxOwed = inputs.BalanceToConvert * inputs.CurrentTaxRate;

            decimal convertNowAfterTax;
            decimal preservedOutsideCashAfterTax = 0m;

            if (inputs.PayTaxFromOutsideCash)
            {
                // Full balance converts and grows tax-free; the tax bill comes from cash that
                // would otherwise have sat in a taxable account, so credit Don't-Convert with
                // what that cash would have grown to instead (same LTCG-at-the-end
                // simplification as the reinvested-savings leg above).
                convertNowAfterTax = inputs.BalanceToConvert * growthFactor;
                decimal taxableGross = taxOwed * growthFactor;
                decimal taxableGain = taxableGross - taxOwed;
                preservedOutsideCashAfterTax = taxOwed + taxableGain * (1 - inputs.CapitalGainsRate);
            }
            else
            {
                // Simpler, self-contained comparison: the tax bill comes out of the balance
                // itself, so less actually starts compounding tax-free.
                decimal netConverted = inputs.BalanceToConvert - taxOwed;
                convertNowAfterTax = netConverted * growthFactor;
            }

            decimal dontConvertGross = inputs.BalanceToConvert * growthFactor;
            decimal dontConvertAfterTax = dontConvertGross * (1 - inputs.FutureTaxRate);

            return new ConversionResult(convertNowAfterTax, dontConvertAfterTax, preservedOutsideCashAfterTax);
        }

        // decimal has no built-in integer-exponent Pow — a loop keeps this in decimal precision
        // throughout instead of round-tripping through double for currency math.
        private static decimal Pow(decimal baseValue, int exponent)
        {
            decimal result = 1m;
            for (int i = 0; i < exponent; i++)
                result *= baseValue;
            return result;
        }
    }
}
