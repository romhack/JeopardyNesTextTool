using System;
using System.Globalization;
using System.Windows.Data;

namespace JeopardyNesTextTool.ViewModel
{
    public class TerminatedToUnterminatedStringConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value)?.TrimEnd('\r');
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (string)value + '\r';
        }
    }

    public class ShortPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const int trimLength = 64;
            var fullPath = (string)value;
            if (fullPath is null)
            {
                return string.Empty;
            }
            return fullPath.Length > trimLength ? $"...{fullPath.Substring(fullPath.Length - trimLength)}" : fullPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
