using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Reckoner.Controls
{
    public partial class PercentageInput : UserControl
    {
        // Backing model value: 0.00–1.00 (e.g. 0.125m)
        public static readonly StyledProperty<decimal> ValueProperty =
            AvaloniaProperty.Register<PercentageInput, decimal>(
                nameof(Value), 0m, defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        // Display settings
        public static readonly StyledProperty<string> FormatStringProperty =
            AvaloniaProperty.Register<PercentageInput, string>(nameof(FormatString), "0.00");

        public static readonly StyledProperty<string?> WatermarkProperty =
            AvaloniaProperty.Register<PercentageInput, string?>(nameof(Watermark), "12.50");

        public static readonly StyledProperty<string?> ErrorMessageProperty =
            AvaloniaProperty.Register<PercentageInput, string?>(nameof(ErrorMessage),
                "Enter a percent from 0 to 100 (example: 12.50).");

        public static readonly StyledProperty<decimal> MinPercentProperty =
            AvaloniaProperty.Register<PercentageInput, decimal>(nameof(MinPercent), 0m);

        public static readonly StyledProperty<decimal> MaxPercentProperty =
            AvaloniaProperty.Register<PercentageInput, decimal>(nameof(MaxPercent), 100m);

        public static readonly StyledProperty<bool> ClampToRangeProperty =
            AvaloniaProperty.Register<PercentageInput, bool>(nameof(ClampToRange), true);

        public static readonly StyledProperty<double> BoxWidthProperty =
            AvaloniaProperty.Register<PercentageInput, double>(nameof(BoxWidth), 80);

        private decimal _lastGoodPercent; // in 0..100

        public decimal Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string FormatString
        {
            get => GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public string? ErrorMessage
        {
            get => GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public decimal MinPercent
        {
            get => GetValue(MinPercentProperty);
            set => SetValue(MinPercentProperty, value);
        }

        public decimal MaxPercent
        {
            get => GetValue(MaxPercentProperty);
            set => SetValue(MaxPercentProperty, value);
        }

        public bool ClampToRange
        {
            get => GetValue(ClampToRangeProperty);
            set => SetValue(ClampToRangeProperty, value);
        }

        public double BoxWidth
        {
            get => GetValue(BoxWidthProperty);
            set => SetValue(BoxWidthProperty, value);
        }

        public PercentageInput()
        {
            InitializeComponent();

            // initialize from Value (0..1) into percent (0..100)
            var pct = Value * 100m;
            _lastGoodPercent = pct;
            PART_TextBox.Text = pct.ToString(FormatString, CultureInfo.CurrentCulture);

            // keep display updated if Value changes externally
            this.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValueProperty)
                {
                    var p = Value * 100m;
                    _lastGoodPercent = p;
                    PART_Error.IsVisible = false;
                    PART_TextBox.Text = p.ToString(FormatString, CultureInfo.CurrentCulture);
                }
            };
        }

        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            var input = (PART_TextBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(input))
            {
                // revert (your model is non-nullable)
                RestoreLastGood();
                PART_Error.IsVisible = false;
                return;
            }

            if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var pct))
            {
                PART_Error.IsVisible = true;
                RestoreLastGood();
                return;
            }

            // Range enforcement
            if (pct < MinPercent || pct > MaxPercent)
            {
                if (ClampToRange)
                    pct = pct < MinPercent ? MinPercent : MaxPercent;
                else
                {
                    PART_Error.IsVisible = true;
                    RestoreLastGood();
                    return;
                }
            }

            PART_Error.IsVisible = false;
            _lastGoodPercent = pct;

            // Commit back to model (0..1)
            Value = pct / 100m;

            // Normalize display
            PART_TextBox.Text = pct.ToString(FormatString, CultureInfo.CurrentCulture);
        }

        private void RestoreLastGood()
        {
            PART_TextBox.Text = _lastGoodPercent.ToString(FormatString, CultureInfo.CurrentCulture);
        }
    }
}
