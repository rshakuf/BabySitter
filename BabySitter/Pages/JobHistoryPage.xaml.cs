using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BabySitter.Pages
{
    public partial class JobHistoryPage : Page
    {
        private readonly ApiService api = new ApiService();

        public JobHistoryPage()
        {
            InitializeComponent();
            Loaded += JobHistoryPage_Loaded;
        }

        private async void JobHistoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            bool isBabysitter = LogInComputer.WhoAmI == "babysitter";

            SubtitleText.Text = isBabysitter
                ? "כל המשמרות שסיימת"
                : "כל הבייביסיטרים שהעסקת";

            try
            {
                var allReqs = await api.GetAllRequestsAsync() ?? new List<Requests>();

                // Past = date is before today, status was approved (completed job)
                List<Requests> history;
                if (isBabysitter)
                {
                    var user = (BabySitterTeens)LogInComputer.CurrentUser;
                    history = allReqs
                        .Where(r => r.BabysitterId?.Id == user.Id
                                 && r.TimeOfRequest.Date < DateTime.Today
                                 && (r.Status == "approved" || r.Status == "cancelled_by_babysitter"))
                        .OrderByDescending(r => r.TimeOfRequest)
                        .ToList();
                }
                else
                {
                    var user = (Parents)LogInComputer.CurrentUser;
                    history = allReqs
                        .Where(r => r.ParentsId?.Id == user.Id
                                 && r.TimeOfRequest.Date < DateTime.Today
                                 && (r.Status == "approved" || r.Status == "cancelled_by_babysitter"))
                        .OrderByDescending(r => r.TimeOfRequest)
                        .ToList();
                }

                LoadingPanel.Visibility = Visibility.Collapsed;

                if (history.Count == 0)
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    TotalJobsText.Text      = "0";
                    TotalHoursText.Text     = "0";
                    LastJobText.Text        = "—";
                    TotalEarningsText.Text  = "₪0";
                    TotalEarningsLabel.Text = isBabysitter ? "סך הכל הכנסה" : "סך הכל הוצאה";
                    PricePerHourPanel.Visibility = Visibility.Collapsed;
                    return;
                }

                // Hourly rate for babysitter view
                int babysitterHourlyRate = 0;
                if (isBabysitter && LogInComputer.CurrentUser is BabySitterTeens bst)
                    babysitterHourlyRate = bst.PriceForAnHour;

                // Summary stats
                int completedOnly = history.Count(r => r.Status == "approved");
                int totalHours    = history.Where(r => r.Status == "approved").Sum(r => r.LenghtTime);

                int totalEarnings = history
                    .Where(r => r.Status == "approved")
                    .Sum(r => r.LenghtTime * (isBabysitter
                        ? babysitterHourlyRate
                        : (r.BabysitterId?.PriceForAnHour ?? 0)));

                TotalJobsText.Text  = completedOnly.ToString();
                TotalHoursText.Text = totalHours.ToString();
                LastJobText.Text    = history.First().TimeOfRequest.ToString("dd/MM/yyyy");

                if (isBabysitter && babysitterHourlyRate > 0)
                {
                    PricePerHourText.Text    = $"₪{babysitterHourlyRate}";
                    PricePerHourPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    PricePerHourPanel.Visibility = Visibility.Collapsed;
                }

                TotalEarningsText.Text  = $"₪{totalEarnings}";
                TotalEarningsLabel.Text = isBabysitter ? "סך הכל הכנסה" : "סך הכל הוצאה";

                // Build cards
                foreach (var req in history)
                    HistoryPanel.Children.Add(BuildCard(req, isBabysitter, isBabysitter
                        ? babysitterHourlyRate
                        : (req.BabysitterId?.PriceForAnHour ?? 0)));

                ListScroll.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show("שגיאה בטעינת היסטוריה: " + ex.Message);
            }
        }

        private UIElement BuildCard(Requests req, bool isBabysitter, int pricePerHour)
        {
            bool cancelled = req.Status == "cancelled_by_babysitter";

            // Status badge colours
            var badgeBg   = cancelled ? new SolidColorBrush(Color.FromRgb(255, 243, 224))
                                      : new SolidColorBrush(Color.FromRgb(232, 245, 233));
            var badgeFg   = cancelled ? new SolidColorBrush(Color.FromRgb(230, 81, 0))
                                      : new SolidColorBrush(Color.FromRgb(27, 94, 32));
            string badge  = cancelled ? "בוטל ע\"י הבייביסיטר" : "הושלם ✓";

            // Other-party name
            string otherName = isBabysitter
                ? $"{req.ParentsId?.FirstName} {req.ParentsId?.LastName}"
                : $"{req.BabysitterId?.FirstName} {req.BabysitterId?.LastName}";
            string otherLabel = isBabysitter ? "הורה:" : "בייביסיטר:";

            // Time display
            string date     = req.TimeOfRequest.ToString("dd/MM/yyyy");
            string timeFrom = req.TimeOfRequest.ToString("HH:mm");
            string timeTo   = req.LenghtTime > 0
                ? req.TimeOfRequest.AddHours(req.LenghtTime).ToString("HH:mm")
                : "—";

            var card = new Border
            {
                Background       = Brushes.White,
                CornerRadius     = new CornerRadius(16),
                Margin           = new Thickness(0, 0, 0, 12),
                Padding          = new Thickness(20, 16, 20, 16),
            };
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8, ShadowDepth = 2, Opacity = 0.06
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: date + details
            var left = new StackPanel();

            // Date row
            var dateRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            dateRow.Children.Add(new TextBlock
            {
                Text = "📅 " + date,
                FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(29, 27, 32))
            });
            dateRow.Children.Add(new TextBlock
            {
                Text = $"  {timeFrom} – {timeTo}",
                FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(73, 69, 79)), VerticalAlignment = VerticalAlignment.Center
            });
            left.Children.Add(dateRow);

            // Other-party
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            nameRow.Children.Add(new TextBlock { Text = otherLabel + " ", FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(103, 80, 164)) });
            nameRow.Children.Add(new TextBlock { Text = otherName,        FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(29, 27, 32)) });
            left.Children.Add(nameRow);

            // Hours + earnings
            if (req.LenghtTime > 0)
            {
                left.Children.Add(new TextBlock
                {
                    Text = $"⏱ {req.LenghtTime} שעות",
                    FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(73, 69, 79))
                });

                if (!cancelled && pricePerHour > 0)
                {
                    int shiftEarnings = req.LenghtTime * pricePerHour;
                    left.Children.Add(new TextBlock
                    {
                        Text = $"💰 ₪{shiftEarnings}",
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0, 137, 123)),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
            }

            Grid.SetColumn(left, 0);
            row.Children.Add(left);

            // Right: badge
            var badgeBorder = new Border
            {
                Background   = badgeBg,
                CornerRadius = new CornerRadius(20),
                Padding      = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = new TextBlock
            {
                Text       = badge,
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = badgeFg
            };
            Grid.SetColumn(badgeBorder, 1);
            row.Children.Add(badgeBorder);

            card.Child = row;
            return card;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
