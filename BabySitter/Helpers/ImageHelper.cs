using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BabySitter.Helpers
{
    public static class ImageHelper
    {
        /// <summary>Decodes a base64 string into a BitmapImage, or null if invalid.</summary>
        public static BitmapImage BitmapFromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var bmp = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bmp.BeginInit();
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        /// <summary>
        /// Applies a profile image (or falls back to the initial letter) on a standard avatar trio:
        ///   avatarBackground  – Ellipse with solid purple fill (always visible behind)
        ///   avatarLetter      – TextBlock showing first initial
        ///   avatarImageEllipse – Ellipse whose Fill is an ImageBrush
        ///   imageBrush        – The ImageBrush inside that ellipse
        /// </summary>
        public static void ApplyAvatar(string base64, string firstName,
            TextBlock avatarLetter, Ellipse avatarImageEllipse, ImageBrush imageBrush)
        {
            var bmp = BitmapFromBase64(base64);
            if (bmp != null)
            {
                imageBrush.ImageSource     = bmp;
                avatarImageEllipse.Visibility = Visibility.Visible;
                avatarLetter.Visibility      = Visibility.Collapsed;
            }
            else
            {
                avatarImageEllipse.Visibility = Visibility.Collapsed;
                avatarLetter.Visibility      = Visibility.Visible;
                avatarLetter.Text = (firstName?.Length > 0) ? firstName[0].ToString() : "?";
            }
        }
    }
}
