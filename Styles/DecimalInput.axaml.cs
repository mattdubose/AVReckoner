using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Reckoner.Controls
{
    public partial class DecimalInput : UserControl
    {
        public static readonly StyledProperty<decimal> ValueProperty =
            AvaloniaProperty.Register<DecimalInput, decimal>(
                nameof(Value),
                defaultValue: 0m,
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string?> WatermarkProperty =
            AvaloniaProperty.Register<DecimalInput, string?>(nameof(Watermark), "0.00");

        public static readonly StyledProperty<string?> ErrorMessageProperty =
            AvaloniaProperty.Register<DecimalInput, string?>(nameof(ErrorMessage),
                "Please enter a decimal number (example: 1000.00).");

        public static readonly StyledProperty<string> FormatStringProperty =
            AvaloniaProperty.Register<DecimalInput, string>(nameof(FormatString), "0.00");

        public static readonly StyledProperty<bool> FormatOnCommitProperty =
            AvaloniaProperty.Register<DecimalInput, bool>(nameof(FormatOnCommit), true);

        // Optional: limit decimals (e.g., 2 for money, maybe 2 for percent too)
        public static readonly StyledProperty<int> MaxDecimalsProperty =
            AvaloniaProperty.Register<DecimalInput, int>(nameof(MaxDecimals), 2);

        private decimal _lastGoodValue;

        public decimal Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
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

        public string FormatString
        {
            get => GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public bool FormatOnCommit
        {
            get => GetValue(FormatOnCommitProperty);
            set => SetValue(FormatOnCommitProperty, value);
        }

        public int MaxDecimals
        {
            get => GetValue(MaxDecimalsProperty);
            set => SetValue(MaxDecimalsProperty, value);
        }

        public DecimalInput()
        {
            InitializeComponent();

            _lastGoodValue = Value;

            // Keep textbox display in sync when Value changes externally
            this.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValueProperty)
                {
                    var v = Value;
                    _lastGoodValue = v;
                    PART_Error.IsVisible = false;

                    PART_TextBox.Text = FormatOnCommit
                        ? v.ToString(FormatString, CultureInfo.CurrentCulture)
                        : v.ToString(CultureInfo.CurrentCulture);
                }
            };
        }

        private void OnLostFocus(object? sender, RoutedEventArgs e)
        {
            var input = (PART_TextBox.Text ?? "").Trim();

            // Empty => revert (since Value is non-nullable)
            if (string.IsNullOrEmpty(input))
            {
                PART_Error.IsVisible = false;
                RestoreLastGood();
                return;
            }

            if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed))
            {
                PART_Error.IsVisible = true;
                RestoreLastGood();
                return;
            }

            // Optional: enforce max decimals
            if (MaxDecimals >= 0)
            {
                var decSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                var idx = input.IndexOf(decSep, System.StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var decimals = input.Length - idx - decSep.Length;
                    if (decimals > MaxDecimals)
                    {
                        PART_Error.Text = $"Use at most {MaxDecimals} decimal places (example: 1000.00).";
                        PART_Error.IsVisible = true;
                        RestoreLastGood();
                        return;
                    }
                }
            }

            // Valid => commit + normalize
            PART_Error.IsVisible = false;
            _lastGoodValue = parsed;
            Value = parsed;

            if (FormatOnCommit)
                PART_TextBox.Text = parsed.ToString(FormatString, CultureInfo.CurrentCulture);
        }

        private void RestoreLastGood()
        {
            PART_TextBox.Text = FormatOnCommit
                ? _lastGoodValue.ToString(FormatString, CultureInfo.CurrentCulture)
                : _lastGoodValue.ToString(CultureInfo.CurrentCulture);
        }
    }
}
