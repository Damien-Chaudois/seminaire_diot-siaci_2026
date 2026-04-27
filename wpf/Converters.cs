using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace wpf;

/// <summary>true → Visible, false → Collapsed</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → Collapsed, false → Visible</summary>
public sealed class BoolToVisibilityInverseConverter : IValueConverter
{
    public static readonly BoolToVisibilityInverseConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Truncates a string to ConverterParameter characters.</summary>
public sealed class TruncateConverter : IValueConverter
{
    public static readonly TruncateConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text) return string.Empty;
        int max = parameter is string p && int.TryParse(p, out var n) ? n : 60;
        return text.Length <= max ? text : text[..max] + "…";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
