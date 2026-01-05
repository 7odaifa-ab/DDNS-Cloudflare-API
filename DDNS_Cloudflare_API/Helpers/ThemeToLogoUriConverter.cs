using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace DDNS_Cloudflare_API.Helpers
{
    internal class ThemeToLogoUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ApplicationTheme theme)
            {
                return "pack://application:,,,/Assets/logo-NoBox@4x.png";
            }

            return theme == ApplicationTheme.Light
                ? "pack://application:,,,/Assets/logo-NoBox@4x.png"
                : "pack://application:,,,/Assets/logo-light-NoBox@4x.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
