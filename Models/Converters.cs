using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FilesContentFinder.Models
{
    public class ValueEqualsToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) return Visibility.Collapsed;

            bool isEqual = string.Equals(value?.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            return (isEqual ^ Invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}