using ClApi;
using Model;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Helpers
{
    public static class ParentProfileHelper
    {
        public static void ShowProfile(Parents parent, Window owner)
        {
            if (parent == null) return;

            var dlg = new Window
            {
                Width  = 400,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner  = owner,
                ResizeMode  = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
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

            // ── Header ────────────────────────────────────────────────────
            var header = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding      = new Thickness(24, 16, 24, 16)
            };
            header.Child = new TextBlock
            {
                Text       = "👤  פרופיל הורה",
                FontSize   = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            main.Children.Add(header);

            // ── Body ──────────────────────────────────────────────────────
            var body = new StackPanel { Margin = new Thickness(24, 18, 24, 22) };

            // Avatar + name row
            var nameRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 0, 0, 16)
            };

            string initial = !string.IsNullOrWhiteSpace(parent.FirstName)
                ? parent.FirstName[0].ToString().ToUpper() : "?";

            var avatar = new Border
            {
                Width        = 56,
                Height       = 56,
                CornerRadius = new CornerRadius(28),
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                Margin       = new Thickness(0, 0, 16, 0)
            };
            avatar.Child = new TextBlock
            {
                Text                = initial,
                FontSize            = 26,
                FontWeight          = FontWeights.Bold,
                Foreground          = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            };
            nameRow.Children.Add(avatar);

            var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            nameStack.Children.Add(new TextBlock
            {
                Text       = $"{parent.FirstName} {parent.LastName}".Trim(),
                FontSize   = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F"))
            });
            if (!string.IsNullOrWhiteSpace(parent.CityNameId?.CityName))
                nameStack.Children.Add(new TextBlock
                {
                    Text       = parent.CityNameId.CityName,
                    FontSize   = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                    Margin     = new Thickness(0, 3, 0, 0)
                });
            nameRow.Children.Add(nameStack);
            body.Children.Add(nameRow);

            // Info chips (phone + kid count — count filled in after async load)
            var infoRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 0, 0, 18)
            };
            if (!string.IsNullOrWhiteSpace(parent.Telephone))
                infoRow.Children.Add(InfoChip("📱", "טלפון", parent.Telephone));
            body.Children.Add(infoRow);

            // ── Children section (loaded async) ───────────────────────────
            var kidsHeader = new TextBlock
            {
                Text       = "פרטי הילדים",
                FontSize   = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                Margin     = new Thickness(0, 0, 0, 8)
            };
            body.Children.Add(kidsHeader);

            var kidsPanel = new StackPanel();
            var loadingText = new TextBlock
            {
                Text       = "טוען...",
                FontSize   = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E"))
            };
            kidsPanel.Children.Add(loadingText);
            body.Children.Add(kidsPanel);

            // Close button
            var closeBtn = new Button
            {
                Content             = "סגור",
                Width               = 120,
                Height              = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background          = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                Foreground          = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                FontSize            = 14,
                FontWeight          = FontWeights.SemiBold,
                BorderThickness     = new Thickness(0),
                Cursor              = System.Windows.Input.Cursors.Hand,
                Margin              = new Thickness(0, 16, 0, 0)
            };
            var bs = new Style(typeof(Border));
            bs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(20)));
            closeBtn.Resources[typeof(Border)] = bs;
            closeBtn.Click += (s, e) => dlg.Close();
            body.Children.Add(closeBtn);

            main.Children.Add(body);
            outer.Child = main;
            dlg.Content = outer;

            // ── Fetch children once dialog is visible ─────────────────────
            dlg.Loaded += async (s, e) =>
            {
                try
                {
                    var api      = new ApiService();
                    var allKids  = await api.GetAllChildrenOfParentsAsync();
                    var myKids   = allKids?
                        .Where(c => c.IdParent?.Id == parent.Id)
                        .OrderBy(c => c.DateOfBirth)
                        .ToList();

                    kidsPanel.Children.Clear();

                    // Update kids chip with the real count from the API
                    int kidCount = myKids?.Count ?? 0;
                    if (kidCount > 0)
                        infoRow.Children.Add(InfoChip("👶", "ילדים", kidCount.ToString()));

                    if (myKids == null || myKids.Count == 0)
                    {
                        kidsPanel.Children.Add(new TextBlock
                        {
                            Text       = "אין ילדים רשומים",
                            FontSize   = 12,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E"))
                        });
                        return;
                    }

                    foreach (var kid in myKids)
                    {
                        string ageText = AgeString(kid.DateOfBirth);
                        string name    = kid.FirstName?.Trim() ?? "";

                        var row = new Border
                        {
                            Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F2FA")),
                            CornerRadius    = new CornerRadius(10),
                            Padding         = new Thickness(14, 8, 14, 8),
                            Margin          = new Thickness(0, 0, 0, 6)
                        };

                        var rowGrid = new Grid();
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var nameTb = new TextBlock
                        {
                            Text       = "👦 " + name,
                            FontSize   = 13,
                            FontWeight = FontWeights.SemiBold,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F")),
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        var ageTb = new TextBlock
                        {
                            Text       = ageText,
                            FontSize   = 12,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                            FontWeight = FontWeights.SemiBold,
                            VerticalAlignment = VerticalAlignment.Center
                        };

                        Grid.SetColumn(nameTb, 0);
                        Grid.SetColumn(ageTb,  1);
                        rowGrid.Children.Add(nameTb);
                        rowGrid.Children.Add(ageTb);
                        row.Child = rowGrid;
                        kidsPanel.Children.Add(row);
                    }
                }
                catch
                {
                    kidsPanel.Children.Clear();
                    kidsPanel.Children.Add(new TextBlock
                    {
                        Text       = "לא ניתן לטעון את פרטי הילדים",
                        FontSize   = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C53030"))
                    });
                }
            };

            dlg.ShowDialog();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string AgeString(DateTime dob)
        {
            if (dob == default) return "";

            var today   = DateTime.Today;
            int months  = (today.Year - dob.Year) * 12 + today.Month - dob.Month;
            if (today.Day < dob.Day) months--;

            if (months < 1)  return "פחות מחודש";
            if (months < 12) return $"{months} חודשים";

            int years = months / 12;
            int rem   = months % 12;
            if (rem == 0) return $"גיל {years}";
            return $"גיל {years} וחצי";   // rough half-year label
        }

        private static Border InfoChip(string icon, string label, string value)
        {
            var chip = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F2FA")),
                CornerRadius = new CornerRadius(12),
                Padding      = new Thickness(14, 10, 14, 10),
                Margin       = new Thickness(0, 0, 10, 0)
            };
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(new TextBlock
            {
                Text                = icon,
                FontSize            = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            stack.Children.Add(new TextBlock
            {
                Text                = label,
                FontSize            = 11,
                Foreground          = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(0, 2, 0, 0)
            });
            stack.Children.Add(new TextBlock
            {
                Text                = value,
                FontSize            = 13,
                FontWeight          = FontWeights.SemiBold,
                Foreground          = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F")),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            chip.Child = stack;
            return chip;
        }
    }
}
