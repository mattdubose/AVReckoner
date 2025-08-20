using Avalonia.Data.Converters;
using System;

namespace Reckoner.Converters
{
    public class BooleanToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 100 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PercentToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is decimal d)
            {
                d = d * 100;
                return d.ToString("F2", culture);
            }
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (decimal.TryParse(value as string, NumberStyles.Number, culture, out var result))
            {
                result /= 100;
                return result;
            }
            return 0m; // or Binding.DoNothing / throw / default fallback
        }
    }
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool flag)
                return !flag;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool flag)
                return !flag;
            return false;
        }
    }
    public class PlayPauseTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? "▶ Run Simulation" : "⏸ Pause";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class NullToTextConverter : IValueConverter
    {
        public string NullText { get; set; } = "NULL";
        public string NotNullText { get; set; } = "OK";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? NullText : NotNullText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
