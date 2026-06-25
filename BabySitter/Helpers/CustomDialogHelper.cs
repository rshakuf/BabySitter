using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Helpers
{
    public static class CustomDialogHelper
    {
        // ── Public API ────────────────────────────────────────────────────────

        public static void ShowError(string message, Window owner = null)
            => ShowDialog("שגיאה", "❌  " + message, "#C62828", "#FFEBEE", owner, false);

        public static void ShowWarning(string message, Window owner = null)
            => ShowDialog("שים לב", "⚠️  " + message, "#E65100", "#FFF3E0", owner, false);

        public static void ShowSuccess(string message, Window owner = null)
            => ShowDialog("הצלחה", "✅  " + message, "#2E7D32", "#E8F5E9", owner, false);

        public static void ShowInfo(string message, Window owner = null)
            => ShowDialog("מידע", "ℹ️  " + message, "#6750A4", "#F3E5F5", owner, false);

        /// <summary>Returns true if the user clicked "כן".</summary>
        public static bool ShowConfirm(string message, string title = "אישור", Window owner = null)
        {
            bool result = false;

            var dlg = BuildWindow(440, 260, owner);

            var outer = BuildOuter();
            var main  = new StackPanel();

            main.Children.Add(BuildHeader(title, "#6750A4", "#EDE7F6", "❓"));

            var body = new StackPanel { Margin = new Thickness(28, 20, 28, 20) };
            body.Children.Add(new TextBlock
            {
                Text         = message,
                FontSize     = 14,
                Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 0, 0, 24)
            });

            var btnRow = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesBtn = MakeButton("כן", "#6750A4", "White", 120);
            yesBtn.Click += (s, e) => { result = true; dlg.Close(); };

            var noBtn = MakeButton("לא, חזור", "#EEEEEE", "#424242", 130);
            noBtn.Click += (s, e) => dlg.Close();

            btnRow.Children.Add(yesBtn);
            btnRow.Children.Add(noBtn);
            body.Children.Add(btnRow);

            main.Children.Add(body);
            outer.Child   = main;
            dlg.Content   = outer;
            dlg.ShowDialog();
            return result;
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static void ShowDialog(string title, string message,
                                       string headerColor, string iconBg,
                                       Window owner, bool hasCancel)
        {
            var dlg   = BuildWindow(420, 220, owner);
            var outer = BuildOuter();
            var main  = new StackPanel();

            main.Children.Add(BuildHeader(title, headerColor, iconBg));

            var body = new StackPanel { Margin = new Thickness(28, 18, 28, 20) };
            body.Children.Add(new TextBlock
            {
                Text         = message,
                FontSize     = 14,
                Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(0, 0, 0, 22)
            });

            var okBtn = MakeButton("אישור", headerColor, "White", 120);
            okBtn.HorizontalAlignment = HorizontalAlignment.Center;
            okBtn.Click += (s, e) => dlg.Close();
            body.Children.Add(okBtn);

            main.Children.Add(body);
            outer.Child = main;
            dlg.Content = outer;
            dlg.ShowDialog();
        }

        private static Window BuildWindow(int w, int h, Window owner)
            => new Window
            {
                Width                 = w,
                Height                = h,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = owner,
                ResizeMode            = ResizeMode.NoResize,
                WindowStyle           = WindowStyle.None,
                AllowsTransparency    = true,
                Background            = Brushes.Transparent,
                FlowDirection         = FlowDirection.RightToLeft,
                Topmost               = true
            };

        private static Border BuildOuter()
        {
            var b = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Margin       = new Thickness(10)
            };
            b.Effect = new DropShadowEffect
            {
                BlurRadius = 30, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black
            };
            return b;
        }

        private static Border BuildHeader(string title, string bgColor, string iconBg, string icon = null)
        {
            var header = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding      = new Thickness(24, 14, 24, 14)
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            if (icon != null)
                row.Children.Add(new TextBlock
                {
                    Text              = icon,
                    FontSize          = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 10, 0)
                });
            row.Children.Add(new TextBlock
            {
                Text              = title,
                FontSize          = 18,
                FontWeight        = FontWeights.Bold,
                Foreground        = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = row;
            return header;
        }

        private static Button MakeButton(string text, string bg, string fg, int width)
        {
            var btn = new Button
            {
                Content         = text,
                Width           = width,
                Height          = 42,
                Margin          = new Thickness(6, 0, 6, 0),
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                Foreground      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                FontSize        = 14,
                FontWeight      = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor          = System.Windows.Input.Cursors.Hand
            };
            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            btn.Resources[typeof(Border)] = s;
            return btn;
        }
    }
}
