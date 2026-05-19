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
    public partial class RequestsBabysitter : Page
    {
        private readonly ApiService api = new ApiService();

        public RequestsBabysitter()
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
                var currentBabysitter = LogInComputer.CurrentUser as BabySitterTeens;
                if (currentBabysitter == null) return;

                var all = await api.GetAllRequestsAsync();

                var mine = all
                    .Where(r => r.BabysitterId?.Id == currentBabysitter.Id
                             && r.Status == "pending")
                    .OrderByDescending(r => r.TimeOfRequest)
                    .ToList();

                RequestsPanel.Children.Clear();

                if (!mine.Any())
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                    return;
                }

                foreach (var req in mine)
                    RequestsPanel.Children.Add(BuildCard(req));

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

        private Border BuildCard(Requests req)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(20),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082")),
                BorderThickness = new Thickness(0, 0, 0, 4)
            };
            card.Effect = new DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.06, Color = Colors.Black };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Info
            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            string parentName = req.ParentsId != null
                ? $"{req.ParentsId.FirstName} {req.ParentsId.LastName}"
                : "לא ידוע";

            info.Children.Add(new TextBlock
            {
                Text = $"הורה: {parentName}",
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
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F"))
            });

            // Buttons
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            buttons.Children.Add(MakeButton("אשר ✓", "#43A047", req, ApproveButton_Click));
            buttons.Children.Add(MakeButton("דחה ✗", "#E53935", req, DismissButton_Click));

            Grid.SetColumn(info, 0);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(info);
            grid.Children.Add(buttons);

            card.Child = grid;
            return card;
        }

        private Button MakeButton(string label, string hexColor, Requests req, RoutedEventHandler handler)
        {
            var btn = new Button
            {
                Content = label,
                Width = 95,
                Height = 40,
                Margin = new Thickness(8, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Tag = req,
                Cursor = Cursors.Hand
            };
            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(20)));
            btn.Resources[typeof(Border)] = s;
            btn.Click += handler;
            return btn;
        }

        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req == null) return;

            req.Status = "approved";
            await api.UpdateRequestAsync(req);
            await LoadRequests();
        }

        private async void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req == null) return;

            req.Status = "rejected";
            await api.UpdateRequestAsync(req);
            await LoadRequests();
        }
    }
}
