using BabySitter.Helpers;
using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Pages
{
    public partial class ParentPaymentsPage : Page
    {
        private readonly ApiService _api = new ApiService();

        public ParentPaymentsPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var parent = LogInComputer.CurrentUser as Parents;
                if (parent == null) return;

                var allRequests = await _api.GetAllRequestsAsync() ?? new List<Requests>();

                // Completed jobs for this parent
                var completed = allRequests
                    .Where(r =>
                        r.ParentsId?.Id == parent.Id &&
                        r.BabysitterId != null &&
                        r.Status == "approved" &&
                        r.TimeOfRequest.AddHours(r.LenghtTime > 0 ? r.LenghtTime : 1) <= DateTime.Now)
                    .GroupBy(r => r.Id).Select(g => g.First())
                    .GroupBy(r => (r.TimeOfRequest, r.BabysitterId?.Id, r.ParentsId?.Id)).Select(g => g.First())
                    .ToList();

                LoadingPanel.Visibility = Visibility.Collapsed;

                if (!completed.Any())
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    return;
                }

                // Group by babysitter
                var groups = completed
                    .GroupBy(r => r.BabysitterId?.Id ?? 0)
                    .Select(g => new
                    {
                        Sitter = g.First().BabysitterId,
                        Jobs   = g.OrderByDescending(r => r.TimeOfRequest).ToList(),
                        TotalHours = g.Sum(r => r.LenghtTime > 0 ? r.LenghtTime : 1),
                        TotalPaid  = g.Sum(r => (r.LenghtTime > 0 ? r.LenghtTime : 1) *
                                               (r.BabysitterId?.PriceForAnHour ?? 0))
                    })
                    .OrderByDescending(g => g.TotalPaid)
                    .ToList();

                int grandTotal = groups.Sum(g => g.TotalPaid);
                int totalJobs  = completed.Count;

                TotalAmountText.Text   = $"₪{grandTotal}";
                TotalJobsText.Text     = totalJobs.ToString();
                TotalSittersText.Text  = groups.Count.ToString();

                PaymentsPanel.Visibility = Visibility.Visible;

                foreach (var g in groups)
                {
                    PaymentsPanel.Children.Add(BuildSitterCard(g.Sitter, g.Jobs, g.TotalHours, g.TotalPaid));
                }
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                CustomDialogHelper.ShowError("שגיאה בטעינה: " + ex.Message, Window.GetWindow(this));
            }
        }

        private Border BuildSitterCard(BabySitterTeens sitter, List<Requests> jobs, int totalHours, int totalPaid)
        {
            var card = new Border
            {
                Background   = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(16),
                Margin       = new Thickness(0, 0, 0, 14),
                Padding      = new Thickness(20, 16, 20, 16),
                Effect       = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.06 }
            };

            var root = new StackPanel();

            // ── Header row: avatar · name · total · expand button ──
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // avatar
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // name
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // total pill
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // expand btn

            // Avatar
            var avatarGrid = new Grid { Width = 48, Height = 48, Margin = new Thickness(0, 0, 14, 0) };
            avatarGrid.Children.Add(new System.Windows.Shapes.Ellipse
            {
                Width = 48, Height = 48,
                Fill  = new SolidColorBrush(Color.FromRgb(103, 80, 164))
            });
            avatarGrid.Children.Add(new TextBlock
            {
                Text = sitter?.FirstName?.Length > 0 ? sitter.FirstName[0].ToString() : "?",
                FontSize = 20, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            });
            var bmp = ImageHelper.BitmapFromBase64(sitter?.ProfilePicture);
            if (bmp != null)
            {
                var imgEllipse = new System.Windows.Shapes.Ellipse { Width = 48, Height = 48 };
                imgEllipse.Fill = new ImageBrush { ImageSource = bmp, Stretch = Stretch.UniformToFill };
                avatarGrid.Children.Add(imgEllipse);
            }
            Grid.SetColumn(avatarGrid, 0);

            // Name + subtitle
            var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            nameStack.Children.Add(new TextBlock
            {
                Text = $"{sitter?.FirstName} {sitter?.LastName}",
                FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(28, 27, 31))
            });
            nameStack.Children.Add(new TextBlock
            {
                Text = $"{totalHours} שעות  •  {jobs.Count} משמרות",
                FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(73, 69, 79))
            });
            Grid.SetColumn(nameStack, 1);

            // Total paid pill
            var totalBorder = new Border
            {
                Background        = new SolidColorBrush(Color.FromRgb(237, 233, 255)),
                CornerRadius      = new CornerRadius(14),
                Padding           = new Thickness(14, 8, 14, 8),
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(10, 0, 10, 0)
            };
            totalBorder.Child = new TextBlock
            {
                Text       = $"₪{totalPaid}",
                FontSize   = 18, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(103, 80, 164))
            };
            Grid.SetColumn(totalBorder, 2);

            // Expand / collapse button
            var expandBtn = new Button
            {
                Content         = "פרטים ▼",
                Height          = 32,
                Padding         = new Thickness(12, 0, 12, 0),
                Background      = new SolidColorBrush(Color.FromRgb(237, 233, 255)),
                Foreground      = new SolidColorBrush(Color.FromRgb(103, 80, 164)),
                FontSize        = 12,
                FontWeight      = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor          = System.Windows.Input.Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            var btnStyle = new Style(typeof(Border));
            btnStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(16)));
            expandBtn.Resources[typeof(Border)] = btnStyle;
            Grid.SetColumn(expandBtn, 3);

            header.Children.Add(avatarGrid);
            header.Children.Add(nameStack);
            header.Children.Add(totalBorder);
            header.Children.Add(expandBtn);
            root.Children.Add(header);

            // ── Collapsible detail section ──
            var detailPanel = new StackPanel { Visibility = Visibility.Collapsed };

            detailPanel.Children.Add(new Border
            {
                Height     = 1,
                Background = new SolidColorBrush(Color.FromRgb(230, 224, 233)),
                Margin     = new Thickness(0, 12, 0, 12)
            });

            foreach (var job in jobs)
            {
                int hours = job.LenghtTime > 0 ? job.LenghtTime : 1;
                int price = hours * (job.BabysitterId?.PriceForAnHour ?? 0);

                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };

                var dateText = new TextBlock
                {
                    Text = job.TimeOfRequest.ToString("dd/MM/yyyy  HH:mm"),
                    FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(73, 69, 79)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(dateText, Dock.Right);

                var priceText = new TextBlock
                {
                    Text       = $"₪{price}  ({hours} שע')",
                    FontSize   = 13, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                row.Children.Add(dateText);
                row.Children.Add(priceText);
                detailPanel.Children.Add(row);
            }

            root.Children.Add(detailPanel);

            // Toggle open/close
            bool isOpen = false;
            expandBtn.Click += (s, e) =>
            {
                isOpen = !isOpen;
                detailPanel.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
                expandBtn.Content      = isOpen ? "סגור ▲" : "פרטים ▼";
            };

            card.Child = root;
            return card;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
