using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace LanChat.Converters;

public class ByteArrayToBitmapConverter : IValueConverter
{
    public static readonly ByteArrayToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes && bytes.Length > 0)
        {
            try
            {
                using var stream = new MemoryStream(bytes);
                return new Bitmap(stream);
            }
            catch { }
        }
        else if (value is string path && File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                return new Bitmap(stream);
            }
            catch { }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
