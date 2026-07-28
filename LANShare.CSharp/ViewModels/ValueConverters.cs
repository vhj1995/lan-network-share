using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LANShare.CSharp.Models;

namespace LANShare.CSharp.ViewModels
{
    public class BytesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return TransferInfo.FormatSize(bytes);
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TransferStatus status)
            {
                return status switch
                {
                    TransferStatus.Completed => new SolidColorBrush(Color.FromRgb(46, 204, 113)),   // Green
                    TransferStatus.Transferring => new SolidColorBrush(Color.FromRgb(52, 152, 219)),// Blue
                    TransferStatus.Pending => new SolidColorBrush(Color.FromRgb(241, 196, 15)),     // Yellow
                    TransferStatus.Failed => new SolidColorBrush(Color.FromRgb(231, 76, 60)),       // Red
                    TransferStatus.Canceled => new SolidColorBrush(Color.FromRgb(149, 165, 166)),   // Gray
                    _ => new SolidColorBrush(Color.FromRgb(149, 165, 166))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DirectionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TransferDirection direction)
            {
                return direction == TransferDirection.Upload ? "↑ Upload" : "↓ Download";
            }
            return "•";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
