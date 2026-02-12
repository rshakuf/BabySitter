using ApiInterface;
using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BabySitter.Pages
{
    public partial class Home : Page
    {
        private List<BabySitterTeens> AllBabysitters = new List<BabySitterTeens>();
        private List<City> AllCities = new List<City>();

        public Home()
        {
            InitializeComponent();
            Loaded += Home_Loaded;
        }

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var api = new ApiService();
                var sitters = await api.GetAllBabySitterTeensAsync();
                AllBabysitters = sitters != null ? sitters.ToList() : new List<BabySitterTeens>();

                var cities = await api.GetAllCitiesAsync();
                AllCities = cities != null ? cities.ToList() : new List<City>();

                // Setup Cities
                CityComboBox.Items.Clear();
                CityComboBox.Items.Add("כל הערים");
                foreach (var city in AllCities)
                {
                    CityComboBox.Items.Add(city.CityName);
                }

                CityComboBox.SelectedIndex = 0;
                SortComboBox.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"שגיאה בטעינת נתונים: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            if (BabysittersItems == null || CityComboBox == null || SortComboBox == null ||
                SearchBox == null || AllBabysitters == null) return;

            var filtered = AllBabysitters.AsEnumerable();

            // 1. סינון טקסט
            string searchText = SearchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText) && searchText != "הקלד שם...")
            {
                filtered = filtered.Where(x =>
                    (x.FirstName?.ToLower().Contains(searchText) == true) ||
                    (x.LastName?.ToLower().Contains(searchText) == true));
            }

            // 2. סינון עיר
            if (CityComboBox.SelectedItem != null)
            {
                string selectedCity = CityComboBox.SelectedItem.ToString();
                if (selectedCity != "כל הערים")
                {
                    filtered = filtered.Where(x =>
                        x.CityNameId != null && x.CityNameId.CityName == selectedCity);
                }
            }

            // 3. מיון
            if (SortComboBox.SelectedIndex == 1) // זול ליקר
                filtered = filtered.OrderBy(x => x.PriceForAnHour);
            else if (SortComboBox.SelectedIndex == 2) // יקר לזול
                filtered = filtered.OrderByDescending(x => x.PriceForAnHour);

            BabysittersItems.ItemsSource = filtered.ToList();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "הקלד שם...") { SearchBox.Text = ""; SearchBox.Foreground = Brushes.Black; }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) { SearchBox.Text = "הקלד שם..."; SearchBox.Foreground = Brushes.Gray; }
        }
    }
}