using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Helpers
{
    public static class PriceWarningHelper
    {
        // ── Thresholds ────────────────────────────────────────────────────────────
        public const int LowPriceThreshold  = 15;   // ₪/hour — below this = too low
        public const int HighPriceThreshold = 80;   // ₪/hour — above this = too high

        /// <summary>
        /// Shows a warning dialog when the price is ≥ 80 ₪/hour.
        /// Returns true if the babysitter clicked "הבנתי, המשך" (wants to proceed).
        /// Returns false if they clicked "שנה מחיר" (wants to change).
        /// </summary>
        public static bool ConfirmHighPrice(int price, Window owner)
        {
            bool proceed = false;

            var dlg = new Window
            {
                Width  = 440,
                Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner  = owner,
                ResizeMode    = ResizeMode.NoResize,
                WindowStyle   = WindowStyle.None,
                AllowsTransparency = true,
                Background    = Brushes.Transparent,
                FlowDirection = FlowDirection.RightToLeft
            };

            var outer = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Margin       = new Thickness(10)
            };
            outer.Effect = new DropShadowEffect
            {
                BlurRadius = 30, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black
            };

            var main = new StackPanel();

            // Header strip
            var header = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding      = new Thickness(24, 16, 24, 16)
            };
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
            headerRow.Children.Add(new TextBlock
            {
                Text = "💸",
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            headerRow.Children.Add(new TextBlock
            {
                Text = "מחיר גבוה",
                FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = headerRow;
            main.Children.Add(header);

            // Body
            var body = new StackPanel { Margin = new Thickness(28, 20, 28, 20) };

            body.Children.Add(new TextBlock
            {
                Text = $"המחיר שהזנת הוא ₪{price} לשעה — זה מחיר גבוה.",
                FontSize = 15, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            body.Children.Add(new TextBlock
            {
                Text = "מחיר גבוה עלול להרתיע הורים מלבקש ממך עבודה.\nהאם אתה בטוח שברצונך להמשיך?",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 22)
            });

            // Buttons
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var proceedBtn = new Button
            {
                Content = "הבנתי, המשך",
                Width = 150, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var ps = new Style(typeof(Border));
            ps.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            proceedBtn.Resources[typeof(Border)] = ps;
            proceedBtn.Click += (s, e) => { proceed = true; dlg.Close(); };

            var changeBtn = new Button
            {
                Content = "שנה מחיר",
                Width = 120, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                Foreground = Brushes.White,
                FontSize = 14, FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var cs = new Style(typeof(Border));
            cs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            changeBtn.Resources[typeof(Border)] = cs;
            changeBtn.Click += (s, e) => dlg.Close();

            btnRow.Children.Add(proceedBtn);
            btnRow.Children.Add(changeBtn);
            body.Children.Add(btnRow);

            main.Children.Add(body);
            outer.Child = main;
            dlg.Content = outer;
            dlg.ShowDialog();
            return proceed;
        }

        /// <summary>
        /// Shows a warning dialog when the price is ≤ 30 ₪/hour.
        /// Returns true if the babysitter clicked "הבנתי, המשך" (wants to proceed).
        /// Returns false if they clicked "שנה מחיר" (wants to change).
        /// </summary>
        public static bool ConfirmLowPrice(int price, Window owner)
        {
            bool proceed = false;

            var dlg = new Window
            {
                Width  = 440,
                Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner  = owner,
                ResizeMode    = ResizeMode.NoResize,
                WindowStyle   = WindowStyle.None,
                AllowsTransparency = true,
                Background    = Brushes.Transparent,
                FlowDirection = FlowDirection.RightToLeft
            };

            var outer = new Border
            {
                Background   = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Margin       = new Thickness(10)
            };
            outer.Effect = new DropShadowEffect
            {
                BlurRadius = 30, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black
            };

            var main = new StackPanel();

            // Header strip — blue for low-price warning
            var header = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0")),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding      = new Thickness(24, 16, 24, 16)
            };
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal };
            headerRow.Children.Add(new TextBlock
            {
                Text = "📉",
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            headerRow.Children.Add(new TextBlock
            {
                Text = "מחיר נמוך",
                FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = headerRow;
            main.Children.Add(header);

            // Body
            var body = new StackPanel { Margin = new Thickness(28, 20, 28, 20) };

            body.Children.Add(new TextBlock
            {
                Text = $"המחיר שהזנת הוא ₪{price} לשעה — זה מחיר נמוך.",
                FontSize = 15, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            body.Children.Add(new TextBlock
            {
                Text = "מחיר נמוך מדי עלול לפגוע בתדמיתך המקצועית.\nהאם אתה בטוח שברצונך להמשיך?",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 22)
            });

            // Buttons
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var proceedBtn = new Button
            {
                Content = "הבנתי, המשך",
                Width = 150, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var ps = new Style(typeof(Border));
            ps.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            proceedBtn.Resources[typeof(Border)] = ps;
            proceedBtn.Click += (s, e) => { proceed = true; dlg.Close(); };

            var changeBtn = new Button
            {
                Content = "שנה מחיר",
                Width = 120, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0")),
                Foreground = Brushes.White,
                FontSize = 14, FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            var cs = new Style(typeof(Border));
            cs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            changeBtn.Resources[typeof(Border)] = cs;
            changeBtn.Click += (s, e) => dlg.Close();

            btnRow.Children.Add(proceedBtn);
            btnRow.Children.Add(changeBtn);
            body.Children.Add(btnRow);

            main.Children.Add(body);
            outer.Child = main;
            dlg.Content = outer;
            dlg.ShowDialog();
            return proceed;
        }
    }
}
