using BabySitter.Helpers;
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
        private List<BabySitterRate> _allRates = new();

        // Stored after first load so JobHistoryReloadAsync can rebuild cards without re-fetching requests
        private List<Requests> _history        = new();
        private bool           _isBabysitter;
        private int            _pricePerHour;

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

                // Load rates for parent view (needed to show/hide the rating button)
                if (!isBabysitter)
                    _allRates = (await api.GetAllBabySitterRatesAsync())?.ToList() ?? new List<BabySitterRate>();

                // Past = date is before today, status was approved (completed job)
                List<Requests> history;
                if (isBabysitter)
                {
                    var user = (BabySitterTeens)LogInComputer.CurrentUser;
                    history = allReqs
                        .Where(r => r.BabysitterId?.Id == user.Id
                                 && (r.TimeOfRequest.Date < DateTime.Today ||
                                     (r.Status == "approved" &&
                                      r.TimeOfRequest.AddHours(r.LenghtTime > 0 ? r.LenghtTime : 1) <= DateTime.Now))
                                 && (r.Status == "approved" || r.Status == "cancelled_by_babysitter"))
                        // Deduplicate: first by Id (exact DB duplicates), then by logical key
                        .GroupBy(r => r.Id).Select(g => g.First())
                        .GroupBy(r => (r.TimeOfRequest, r.BabysitterId?.Id, r.ParentsId?.Id)).Select(g => g.First())
                        .OrderByDescending(r => r.TimeOfRequest)
                        .ToList();
                }
                else
                {
                    var user = (Parents)LogInComputer.CurrentUser;
                    history = allReqs
                        .Where(r => r.ParentsId?.Id == user.Id
                                 && (r.TimeOfRequest.Date < DateTime.Today ||
                                     (r.Status == "approved" &&
                                      r.TimeOfRequest.AddHours(r.LenghtTime > 0 ? r.LenghtTime : 1) <= DateTime.Now))
                                 && (r.Status == "approved" || r.Status == "cancelled_by_babysitter"))
                        // Deduplicate: first by Id (exact DB duplicates), then by logical key
                        .GroupBy(r => r.Id).Select(g => g.First())
                        .GroupBy(r => (r.TimeOfRequest, r.BabysitterId?.Id, r.ParentsId?.Id)).Select(g => g.First())
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

                // Store for use by JobHistoryReloadAsync (rate updates don't change the request list)
                _history        = history;
                _isBabysitter   = isBabysitter;
                _pricePerHour   = babysitterHourlyRate;

                // Clear previous cards before rebuilding (prevents duplicates on reload)
                HistoryPanel.Children.Clear();

                // Build cards
                foreach (var req in history)
                    HistoryPanel.Children.Add(BuildCard(req, isBabysitter, isBabysitter
                        ? babysitterHourlyRate
                        : (req.BabysitterId?.PriceForAnHour ?? 0), _allRates));

                ListScroll.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                CustomDialogHelper.ShowError("שגיאה בטעינת היסטוריה: " + ex.Message, Window.GetWindow(this));
            }
        }

        private UIElement BuildCard(Requests req, bool isBabysitter, int pricePerHour,
                                    List<BabySitterRate> rates = null)
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

            if (!isBabysitter && req.BabysitterId != null)
            {
                // Clickable underlined name — navigates to the babysitter's availability page
                var nameLink = new TextBlock
                {
                    Text                = otherName,
                    FontSize            = 13,
                    Foreground          = new SolidColorBrush(Color.FromRgb(29, 27, 32)),
                    TextDecorations     = TextDecorations.Underline,
                    Cursor              = System.Windows.Input.Cursors.Hand,
                    ToolTip             = "לחץ לצפות בזמינות הבייביסיטר",
                    Tag                 = req.BabysitterId
                };
                nameLink.MouseLeftButtonUp += (s, e) =>
                {
                    var teen = (s as TextBlock)?.Tag as BabySitterTeens;
                    if (teen != null)
                        NavigationService?.Navigate(new AvailabilityPage(teen));
                };
                nameRow.Children.Add(nameLink);
            }
            else
            {
                nameRow.Children.Add(new TextBlock { Text = otherName, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(29, 27, 32)) });
            }
            left.Children.Add(nameRow);

            // Hours + earnings
            if (req.LenghtTime > 0)
            {
                string hoursText = req.LenghtTime == 1 ? "שעה"
                                 : req.LenghtTime == 2 ? "שעתיים"
                                 : $"{req.LenghtTime} שעות";
                left.Children.Add(new TextBlock
                {
                    Text = $"⏱ {hoursText}",
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

            // Right: badge + optional profile button
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

            var rightPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            rightPanel.Children.Add(badgeBorder);

            // Show parent profile button for babysitter view
            if (isBabysitter && req.ParentsId != null)
            {
                var profileBtn = new Button
                {
                    Content    = "👤 פרופיל הורה",
                    Height     = 30,
                    Padding    = new Thickness(10, 0, 10, 0),
                    Margin     = new Thickness(0, 8, 0, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                    FontSize   = 12,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Cursor     = System.Windows.Input.Cursors.Hand,
                    Tag        = req.ParentsId
                };
                var ps = new Style(typeof(Border));
                ps.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(15)));
                profileBtn.Resources[typeof(Border)] = ps;
                profileBtn.Click += (s, e) =>
                {
                    var p = (s as Button)?.Tag as Parents;
                    ParentProfileHelper.ShowProfile(p, Window.GetWindow(this));
                };
                rightPanel.Children.Add(profileBtn);
            }

            // Rating section — parent view, completed jobs only
            if (!isBabysitter && !cancelled && rates != null)
            {
                var currentParent = LogInComputer.CurrentUser as Parents;
                var existingRate  = rates.FirstOrDefault(r =>
                    r.IdBabySitter?.Id == req.BabysitterId?.Id &&
                    r.IdParent?.Id     == currentParent?.Id);

                if (existingRate != null)
                {
                    // Show current rating (clickable — lets the parent re-rate)
                    var starsPanel = new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        Margin              = new Thickness(0, 8, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Cursor              = System.Windows.Input.Cursors.Hand,
                        ToolTip             = "לחץ לעדכן דירוג"
                    };
                    var gold = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    var gray = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                    for (int i = 1; i <= 5; i++)
                        starsPanel.Children.Add(new TextBlock
                        {
                            Text       = "★",
                            FontSize   = 20,
                            Foreground = i <= existingRate.Stars ? gold : gray,
                            Margin     = new Thickness(1, 0, 1, 0)
                        });
                    starsPanel.Tag = req;
                    starsPanel.MouseLeftButtonUp += async (s, e) =>
                    {
                        var r = (s as StackPanel)?.Tag as Requests;
                        if (r == null) return;
                        int bsId     = existingRate.IdBabySitter?.Id ?? r.BabysitterId?.Id ?? 0;
                        int parentId = existingRate.IdParent?.Id     ?? (LogInComputer.CurrentUser as Parents)?.Id ?? 0;
                        string name  = r.BabysitterId != null
                            ? $"{r.BabysitterId.FirstName} {r.BabysitterId.LastName}".Trim()
                            : "";
                        var dlg = new RateDialog(name, existingRate.Stars,
                            existingRate.Tags, existingRate.ReviewText)
                        { Owner = Window.GetWindow(this) };
                        if (dlg.ShowDialog() != true) return;
                        try
                        {
                            int saved = await api.UpsertBabySitterRateAsync(new BabySitterRate
                            {
                                Stars        = dlg.SelectedRating,
                                IdBabySitter = new BabySitterTeens { Id = bsId },
                                IdParent     = new Parents         { Id = parentId },
                                DateOfRate   = DateTime.Today,
                                Tags         = dlg.SelectedTags,
                                ReviewText   = dlg.ReviewText
                            });
                            if (saved <= 0) CustomDialogHelper.ShowError("שגיאה בעדכון הדירוג", Window.GetWindow(this));
                            await JobHistoryReloadAsync();
                        }
                        catch (Exception ex)
                        {
                            CustomDialogHelper.ShowError("שגיאה: " + ex.Message, Window.GetWindow(this));
                        }
                    };
                    rightPanel.Children.Add(starsPanel);
                }
                else
                {
                    var rateBtn = new Button
                    {
                        Content         = "דרג ★",
                        Height          = 34,
                        Padding         = new Thickness(14, 0, 14, 0),
                        Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")),
                        Foreground      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                        FontWeight      = FontWeights.SemiBold,
                        FontSize        = 13,
                        BorderThickness = new Thickness(1),
                        BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082")),
                        Margin          = new Thickness(0, 8, 0, 0),
                        Tag             = req,
                        Cursor          = System.Windows.Input.Cursors.Hand
                    };
                    var rs = new Style(typeof(Border));
                    rs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(17)));
                    rateBtn.Resources[typeof(Border)] = rs;
                    rateBtn.Click += async (s, e) =>
                    {
                        var r = (s as Button)?.Tag as Requests;
                        if (r?.BabysitterId == null) return;
                        string name = $"{r.BabysitterId.FirstName} {r.BabysitterId.LastName}".Trim();
                        var dlg = new RateDialog(name) { Owner = Window.GetWindow(this) };
                        if (dlg.ShowDialog() != true) return;
                        try
                        {
                            var parent = LogInComputer.CurrentUser as Parents;
                            int saved = await api.UpsertBabySitterRateAsync(new BabySitterRate
                            {
                                Stars        = dlg.SelectedRating,
                                IdBabySitter = new BabySitterTeens { Id = r.BabysitterId.Id },
                                IdParent     = new Parents         { Id = parent.Id },
                                DateOfRate   = DateTime.Today,
                                Tags         = dlg.SelectedTags,
                                ReviewText   = dlg.ReviewText
                            });
                            if (saved <= 0) CustomDialogHelper.ShowError("שגיאה בשמירת הדירוג", Window.GetWindow(this));
                            await JobHistoryReloadAsync();
                        }
                        catch (Exception ex)
                        {
                            CustomDialogHelper.ShowError("שגיאה: " + ex.Message, Window.GetWindow(this));
                        }
                    };
                    rightPanel.Children.Add(rateBtn);
                }
            }

            Grid.SetColumn(rightPanel, 1);
            row.Children.Add(rightPanel);

            card.Child = row;
            return card;
        }

        /// <summary>
        /// Re-fetches only the rates (requests don't change when a rating is updated),
        /// then rebuilds the cards in-place — fully awaited, no fire-and-forget.
        /// </summary>
        private async System.Threading.Tasks.Task JobHistoryReloadAsync()
        {
            _allRates = (await api.GetAllBabySitterRatesAsync())?.ToList() ?? _allRates;

            HistoryPanel.Children.Clear();
            foreach (var req in _history)
                HistoryPanel.Children.Add(BuildCard(req, _isBabysitter, _isBabysitter
                    ? _pricePerHour
                    : (req.BabysitterId?.PriceForAnHour ?? 0), _allRates));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
