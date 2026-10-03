using System;
using System.IO;

namespace LanChat.Maui.Services;

public static class ImageCompressor
{
    /// <summary>
    /// Compresses and resizes high-resolution smartphone camera photos into optimal chat-sized images (max 1600px, JPEG 82%),
    /// automatically fixing EXIF rotation so photos are never sideways or upside down.
    /// </summary>
    public static byte[] CompressImage(byte[] rawBytes, int maxDimension = 1600, int quality = 82)
    {
        if (rawBytes == null || rawBytes.Length == 0) return rawBytes ?? Array.Empty<byte>();

#if ANDROID
        try
        {
            using var stream = new MemoryStream(rawBytes);

            // 1. Read EXIF orientation
            int rotationDegrees = 0;
            try
            {
                var exif = new Android.Media.ExifInterface(stream);
                int orientation = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);
                rotationDegrees = orientation switch
                {
                    (int)Android.Media.Orientation.Rotate90 => 90,
                    (int)Android.Media.Orientation.Rotate180 => 180,
                    (int)Android.Media.Orientation.Rotate270 => 270,
                    _ => 0
                };
            }
            catch { }

            // 2. Decode image bounds first without allocating full bitmap memory
            var options = new Android.Graphics.BitmapFactory.Options
            {
                InJustDecodeBounds = true
            };
            Android.Graphics.BitmapFactory.DecodeByteArray(rawBytes, 0, rawBytes.Length, options);

            int origWidth = options.OutWidth;
            int origHeight = options.OutHeight;
            if (origWidth <= 0 || origHeight <= 0) return rawBytes;

            // 3. Subsample if image is gigantic (e.g. 50MP/108MP phone camera) to avoid OutOfMemory
            int sampleSize = 1;
            while ((origWidth / sampleSize) > maxDimension * 2 || (origHeight / sampleSize) > maxDimension * 2)
            {
                sampleSize *= 2;
            }

            var decodeOptions = new Android.Graphics.BitmapFactory.Options
            {
                InSampleSize = sampleSize
            };

            using var decodedBitmap = Android.Graphics.BitmapFactory.DecodeByteArray(rawBytes, 0, rawBytes.Length, decodeOptions);
            if (decodedBitmap == null) return rawBytes;

            int currentWidth = decodedBitmap.Width;
            int currentHeight = decodedBitmap.Height;

            float scale = Math.Min(1.0f, Math.Min((float)maxDimension / currentWidth, (float)maxDimension / currentHeight));
            int targetWidth = Math.Max(1, (int)(currentWidth * scale));
            int targetHeight = Math.Max(1, (int)(currentHeight * scale));

            Android.Graphics.Matrix matrix = new Android.Graphics.Matrix();
            if (rotationDegrees != 0)
            {
                matrix.PostRotate(rotationDegrees);
            }

            Android.Graphics.Bitmap finalBitmap;
            if (scale < 1.0f || rotationDegrees != 0)
            {
                using var scaledBitmap = Android.Graphics.Bitmap.CreateScaledBitmap(decodedBitmap, targetWidth, targetHeight, true);
                if (rotationDegrees != 0)
                {
                    finalBitmap = Android.Graphics.Bitmap.CreateBitmap(scaledBitmap, 0, 0, scaledBitmap.Width, scaledBitmap.Height, matrix, true);
                }
                else
                {
                    finalBitmap = scaledBitmap;
                }
            }
            else
            {
                finalBitmap = decodedBitmap;
            }

            using var outStream = new MemoryStream();
            finalBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, outStream);
            return outStream.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Image compression error: {ex.Message}");
            return rawBytes;
        }
#else
        return rawBytes;
#endif
    }
}
