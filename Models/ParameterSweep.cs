namespace Reckoner.Models
{
    // The FWStrategySettings fields a sweep can vary. Kept to the FW strategy's own "knobs" —
    // extend here if a future strategy needs sweeping too.
    public enum SweepParameter
    {
        TriggerToSell,
        TriggerToBuyBack,
        TriggerToBuyRecovery,
        MaxDaysOutOfMarket,
    }

    // One row in the sweep configuration UI — a knob the analyst can turn on and give a range.
    // Min/Max/Step are in *display* units (e.g. 18 meaning 18%, or 30 meaning 30 days); Scale
    // converts to the FWStrategySettings model unit (0.18, or 30) so the UI never shows raw
    // fractions like 0.18.
    public partial class SweepKnob : ObservableObject
    {
        public SweepParameter Parameter { get; }
        public string Label { get; }
        public string Unit { get; }
        private readonly decimal _scale;

        [ObservableProperty] private bool isEnabled;
        [ObservableProperty] private decimal min;
        [ObservableProperty] private decimal max;
        [ObservableProperty] private decimal step;

        public SweepKnob(SweepParameter parameter, string label, string unit, decimal scale,
            decimal currentModelValue, decimal defaultStepDisplay)
        {
            Parameter = parameter;
            Label = label;
            Unit = unit;
            _scale = scale;
            decimal currentDisplay = currentModelValue * scale;
            min = Math.Max(0, currentDisplay - defaultStepDisplay * 2);
            max = currentDisplay + defaultStepDisplay * 2;
            step = defaultStepDisplay;
        }

        // Every value this knob will take, in FWStrategySettings' own units — empty if Step <= 0
        // rather than looping forever.
        public IEnumerable<decimal> ModelValues()
        {
            if (Step <= 0) yield break;
            for (decimal v = Min; v <= Max + 0.0001m; v += Step)
                yield return v / _scale;
        }

        public string FormatModelValue(decimal modelValue) => $"{modelValue * _scale:0}{Unit}";

        public static void Apply(FWStrategySettings settings, SweepParameter parameter, decimal modelValue)
        {
            switch (parameter)
            {
                case SweepParameter.TriggerToSell: settings.TriggerToSell = modelValue; break;
                case SweepParameter.TriggerToBuyBack: settings.TriggerToBuyBack = modelValue; break;
                case SweepParameter.TriggerToBuyRecovery: settings.TriggerToBuyRecovery = modelValue; break;
                case SweepParameter.MaxDaysOutOfMarket: settings.MaxDaysOutOfMarket = (int)modelValue; break;
            }
        }

        public static ObservableCollection<SweepKnob> BuildDefaults(FWStrategySettings current) => new()
        {
            new SweepKnob(SweepParameter.TriggerToSell, "Sell Trigger", "%", 100m, current.TriggerToSell, 2m),
            new SweepKnob(SweepParameter.TriggerToBuyBack, "Buy-Back (Dip) Trigger", "%", 100m, current.TriggerToBuyBack, 2m),
            new SweepKnob(SweepParameter.TriggerToBuyRecovery, "Buy-Back (Recovery) Trigger", "%", 100m, current.TriggerToBuyRecovery, 2m),
            new SweepKnob(SweepParameter.MaxDaysOutOfMarket, "Max Days Out of Market", "d", 1m, current.MaxDaysOutOfMarket, 5m),
        };
    }
}
