using ClApi;
using Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Pages
{
    public partial class RequestsParents : Page
    {
        private readonly ApiService api = new ApiService();
        private System.Collections.Generic.List<BabySitterRate> _allRates = new();

        public RequestsParents()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadRequests();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        private async Task LoadRequests()
        {
            LoadingPanel.Visibility = Visibility.Visible;
            EmptyPanel.Visibility = Visibility.Collapsed;
            RequestsScrollViewer.Visibility = Visibility.Collapsed;

            try
            {
                var currentParent = LogInComputer.CurrentUser as Parents;
                if (currentParent == null) return;

                var allRequests  = await api.GetAllRequestsAsync();
                var allSchedules = await api.GetAllSchedulesAsync();
                _allRates = (await api.GetAllBabySitterRatesAsync())?.ToList()
                            ?? new System.Collections.Generic.List<BabySitterRate>();

                var mine = allRequests
                    .Where(r => r.ParentsId?.Id == currentParent.Id)
                    .OrderByDescending(r => r.TimeOfRequest)
                    .ToList();

                RequestsPanel.Children.Clear();

                if (!mine.Any())
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    return;
                }

                foreach (var req in mine)
                {
                    // Find matching schedule to get end time
                    var matchingSlot = allSchedules?.FirstOrDefault(s =>
                        s.BabysitterId?.Id == req.BabysitterId?.Id &&
                        s.DateAvailable.Date == req.TimeOfRequest.Date &&
                        s.Starttime == TimeOnly.FromTimeSpan(req.TimeOfRequest.TimeOfDay));

                    RequestsPanel.Children.Add(BuildCard(req, matchingSlot));
                }

                RequestsScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת בקשות: " + ex.Message);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private Border BuildCard(Requests req, Schedule matchingSlot = null)
        {
            string statusText;
            Color borderColor;
            bool canCancel;

            switch (req.Status)
            {
                case "approved":
                    statusText = "אושר ✓";
                    borderColor = (Color)ColorConverter.ConvertFromString("#A5D6A7");
                    canCancel = false;
                    break;
                case "rejected":
                    statusText = "נדחה ✗";
                    borderColor = (Color)ColorConverter.ConvertFromString("#EF9A9A");
                    canCancel = false;
                    break;
                case "cancelled_by_babysitter":
                    statusText = "בוטל ע\"י הבייביסיטר ⚠";
                    borderColor = (Color)ColorConverter.ConvertFromString("#FFCC80");
                    canCancel = false;
                    break;
                default: // pending
                    statusText = "ממתין...";
                    borderColor = (Color)ColorConverter.ConvertFromString("#FFE082");
                    canCancel = true;
                    break;
            }

            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(20),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(0, 0, 0, 4)
            };
            card.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.06, Color = Colors.Black };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Info
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            string babysitterName = req.BabysitterId != null
                ? $"{req.BabysitterId.FirstName} {req.BabysitterId.LastName}"
                : "לא ידוע";

            info.Children.Add(new TextBlock
            {
                Text = $"בייביסיטר: {babysitterName}",
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F")),
                Margin = new Thickness(0, 0, 0, 6)
            });
            info.Children.Add(new TextBlock
            {
                Text = $"תאריך מבוקש: {req.TimeOfRequest:dd/MM/yyyy}",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                Margin = new Thickness(0, 0, 0, 3)
            });
            info.Children.Add(new TextBlock
            {
                Text = $"שעת התחלה: {req.TimeOfRequest:HH:mm}",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                Margin = new Thickness(0, 0, 0, 3)
            });

            // Prefer LenghtTime from the request; fall back to matching schedule slot
            if (req.LenghtTime > 0)
            {
                string endTime = (req.TimeOfRequest + TimeSpan.FromHours(req.LenghtTime)).ToString("HH:mm");
                info.Children.Add(new TextBlock
                {
                    Text = $"שעת סיום: {endTime}  ({req.LenghtTime} שעות)",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F"))
                });
            }
            else if (matchingSlot != null)
            {
                int hours = (int)Math.Round((matchingSlot.Endtime - matchingSlot.Starttime).TotalHours);
                info.Children.Add(new TextBlock
                {
                    Text = $"שעת סיום: {matchingSlot.Endtime:HH:mm}  ({hours} שעות)",
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F"))
                });
            }

            // Right: badge + cancel
            var right = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var badge = new Border
            {
                Background = new SolidColorBrush(borderColor),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(16, 8, 16, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, canCancel ? 10 : 0)
            };
            badge.Child = new TextBlock { Text = statusText, FontWeight = FontWeights.Bold, FontSize = 14 };
            right.Children.Add(badge);

            if (canCancel)
            {
                var cancelBtn = new Button
                {
                    Content = "בטל בקשה",
                    Height = 36,
                    Padding = new Thickness(14, 0, 14, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F3")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF9A9A")),
                    Tag = req,
                    Cursor = Cursors.Hand
                };
                var s = new Style(typeof(Border));
                s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(18)));
                cancelBtn.Resources[typeof(Border)] = s;
                cancelBtn.Click += CancelButton_Click;
                right.Children.Add(cancelBtn);
            }

            // Rating section for approved past jobs
            if (req.Status == "approved" && req.TimeOfRequest.Date < DateTime.Today)
            {
                var currentParent = LogInComputer.CurrentUser as Parents;
                var existingRate = _allRates.FirstOrDefault(r =>
                    r.IdBabySitter?.Id == req.BabysitterId?.Id &&
                    r.IdParent?.Id == currentParent?.Id);

                if (existingRate != null)
                {
                    // Show existing stars (read-only)
                    var starsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
                    var gold = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    var gray = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                    for (int i = 1; i <= 5; i++)
                        starsPanel.Children.Add(new TextBlock { Text = "★", FontSize = 18, Foreground = i <= existingRate.Stars ? gold : gray, Margin = new Thickness(1, 0, 1, 0) });
                    right.Children.Add(starsPanel);
                }
                else
                {
                    // Show "rate" button
                    var rateBtn = new Button
                    {
                        Content = "דרג ★",
                        Height = 34,
                        Padding = new Thickness(14, 0, 14, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8E1")),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 13,
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082")),
                        Margin = new Thickness(0, 8, 0, 0),
                        Tag = req,
                        Cursor = Cursors.Hand
                    };
                    var rs = new Style(typeof(Border));
                    rs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(17)));
                    rateBtn.Resources[typeof(Border)] = rs;
                    rateBtn.Click += RateButton_Click;
                    right.Children.Add(rateBtn);
                }
            }

            Grid.SetColumn(info, 0);
            Grid.SetColumn(right, 1);
            grid.Children.Add(info);
            grid.Children.Add(right);

            card.Child = grid;
            return card;
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req == null) return;

            var result = MessageBox.Show("האם לבטל את הבקשה?", "ביטול בקשה",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            await api.DeleteRequestAsync(req.Id);
            await LoadRequests();
        }

        private async void RateButton_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req?.BabysitterId == null) return;

            string babysitterName = $"{req.BabysitterId.FirstName} {req.BabysitterId.LastName}";
            var dialog = new RateDialog(babysitterName) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() != true) return;

            var currentParent = LogInComputer.CurrentUser as Parents;
            var rate = new BabySitterRate
            {
                Stars = dialog.SelectedRating,
                IdBabySitter = req.BabysitterId,
                IdParent = currentParent,
                DateOfRate = DateTime.Today
            };

            await api.InsertBabySitterRateAsync(rate);
            await LoadRequests();
        }
    }
}
