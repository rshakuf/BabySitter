using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; 

namespace BabySitter.Pages
{
    public partial class Home : Page
    {
        private List<BabySitterTeens> AllBabysitters = new List<BabySitterTeens>();
        private List<City> AllCities = new List<City>();
        private Dictionary<int, (double avg, int count)> _ratings = new();

        // Debounce timer — prevents a filter call on every single keystroke
        private readonly DispatcherTimer _searchDebounce;

        public Home()
        {
            InitializeComponent();

            _searchDebounce = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounce.Tick += (s, e) =>
            {
                _searchDebounce.Stop();
                ApplyFilters();
            };

            Loaded += Home_Loaded;
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            ShowWelcomeMessage();
            await LoadData();

            // Refresh ratings whenever navigation returns to this page
            if (NavigationService != null)
                NavigationService.Navigated += async (s, args) =>
                {
                    if (args.Content == this && AllBabysitters.Count > 0)
                        await LoadData();
                };
        }


        // ─── Welcome message ──────────────────────────────────────────────────────

        private void ShowWelcomeMessage()
        {
            if (LogInComputer.CurrentUser is Parents parent)
                WelcomeText.Text = $"שלום, {parent.FirstName} <3";
        }

        // ─── Data loading ─────────────────────────────────────────────────────────

        private async Task LoadData()
        {
            // Show loading, hide everything else
            LoadingPanel.Visibility = Visibility.Visible;
            ResultsScrollViewer.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;

            try
            {
                var api = new ApiService();

                // Load babysitters, cities and ratings in parallel
                var sittersTask = api.GetAllBabySitterTeensAsync();
                var citiesTask  = api.GetAllCitiesAsync();
                var ratesTask   = api.GetAllBabySitterRatesAsync();
                await Task.WhenAll(sittersTask, citiesTask, ratesTask);

                AllBabysitters = sittersTask.Result?.ToList() ?? new List<BabySitterTeens>();
                AllCities      = citiesTask.Result?.ToList()  ?? new List<City>();

                // Build per-babysitter rating lookup
                _ratings = (ratesTask.Result ?? new BabySitterRateList())
                    .Where(r => r.IdBabySitter != null)
                    .GroupBy(r => r.IdBabySitter.Id)
                    .ToDictionary(g => g.Key, g => (avg: g.Average(r => r.Stars), count: g.Count()));

                // Populate city combo
                CityComboBox.Items.Clear();
                CityComboBox.Items.Add("כל הערים");
                foreach (var city in AllCities)
                    CityComboBox.Items.Add(city.CityName);

                CityComboBox.SelectedIndex = 0;
                SortComboBox.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                Helpers.CustomDialogHelper.ShowError($"שגיאה בטעינת נתונים: {ex.Message}", Window.GetWindow(this));
            }
            finally
            {
                // Always hide the loading spinner when done
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        // ─── Filtering & sorting ──────────────────────────────────────────────────

        private void ApplyFilters()
        {
            if (BabysittersItems == null || CityComboBox == null ||
                SortComboBox == null || SearchBox == null || AllBabysitters == null)
                return;

            var filtered = AllBabysitters.AsEnumerable();

            // 1. Text search
            string searchText = SearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(x =>
                    (x.FirstName?.ToLower().Contains(searchText) == true) ||
                    (x.LastName?.ToLower().Contains(searchText) == true));
            }

            // 2. City filter
            if (CityComboBox.SelectedItem != null)
            {
                string selectedCity = CityComboBox.SelectedItem.ToString();
                if (selectedCity != "כל הערים")
                {
                    filtered = filtered.Where(x =>
                        x.CityNameId != null && x.CityNameId.CityName == selectedCity);
                }
            }

            // 3. Sort
            filtered = SortComboBox.SelectedIndex switch
            {
                1 => filtered.OrderBy(x => x.PriceForAnHour),           // זול ליקר
                2 => filtered.OrderByDescending(x => x.PriceForAnHour), // יקר לזול
                _ => filtered
            };

            var result = filtered.ToList();

            // Wrap each babysitter with their computed rating data
            var cardVMs = result.Select(b =>
            {
                _ratings.TryGetValue(b.Id, out var r);
                return new BabySitterCardVM { Teen = b, AverageRating = r.avg, RatingCount = r.count };
            }).ToList();

            BabysittersItems.ItemsSource = cardVMs;

            // Show results list or empty state
            ResultsScrollViewer.Visibility = cardVMs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyPanel.Visibility = cardVMs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Show/hide the clear (✕) button
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            // Debounce: wait 300ms of inactivity before filtering
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            SearchBox.Focus();
        }

        private void MyRequests(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RequestsParents());
        }

        private void GoToMyProfile(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new BabySitter.Pages.MyProfile());
        }

        private void GoToAboutUs(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AboutUs());
        }

        private void GoToHistory(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new JobHistoryPage());
        }

        private void GoToPayments(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ParentPaymentsPage());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogInComputer.CurrentUser = null;
            LogInComputer.WhoAmI     = null;
            NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }
    }
}