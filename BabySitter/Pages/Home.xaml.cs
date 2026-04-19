using ApiInterface;
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

            if (NavigationService != null)
                NavigationService.Navigated += NavigationService_Navigated;

            // Unsubscribe when page unloads to prevent memory leak
            Unloaded += (s, _) =>
            {
                if (NavigationService != null)
                    NavigationService.Navigated -= NavigationService_Navigated;
            };

            await LoadData();
        }

        private async void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Content == this)
                await LoadData();
        }

        // ─── Welcome message ──────────────────────────────────────────────────────

        private void ShowWelcomeMessage()
        {
            if (LogInComputer.CurrentUser == null) return;

            string name = LogInComputer.WhoAmI switch
            {
                "parent" => ((Parents)LogInComputer.CurrentUser).FirstName,
                "babysitter" => ((BabySitterTeens)LogInComputer.CurrentUser).FirstName,
                _ => ""
            };

            if (!string.IsNullOrEmpty(name))
                WelcomeText.Text = $"שלום, {name} <3";
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

                var sitters = await api.GetAllBabySitterTeensAsync();
                AllBabysitters = sitters != null ? sitters.ToList() : new List<BabySitterTeens>();

                var x = await api.GetAllParentsAsync();
                if (x!=null)
                {
                    CityComboBox.Items.Add(x[0]);
                }

                var cities = await api.GetAllCitiesAsync();
                AllCities = cities != null ? cities.ToList() : new List<City>();

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
                MessageBox.Show($"שגיאה בטעינת נתונים: {ex.Message}");
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
            BabysittersItems.ItemsSource = result;

            // Show results list or empty state
            ResultsScrollViewer.Visibility = result.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyPanel.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
            if (LogInComputer.WhoAmI == "parent")
                NavigationService.Navigate(new RequestsParents());
            else if (LogInComputer.WhoAmI == "babysitter")
                NavigationService.Navigate(new RequestsBabysitter());
            else
                MessageBox.Show("לא זוהה סוג משתמש");
        }

        private void GoToMyProfile(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new BabySitter.Pages.MyProfile());
        }
    }
}