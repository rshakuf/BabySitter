using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter.Pages
{
    public partial class ChildOfParents : Page
    {
        private readonly Parents _parent;
        private readonly ApiService _api = new ApiService();
        private int savedKidsCount = 0;

        public ChildOfParents(Parents p)
        {
            InitializeComponent();
            _parent = p;
            Loaded += async (s, e) => await LoadAndBuildAsync();
        }

        private async System.Threading.Tasks.Task LoadAndBuildAsync()
        {
            try
            {
                var cities = await _api.GetAllCitiesAsync();
                CreateKidControls(cities);
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת ערים: " + ex.Message);
            }
        }

        private void CreateKidControls(List<City> cities)
        {
            KidsContainer.Children.Clear();

            for (int i = 0; i < _parent.NumOfKids; i++)
            {
                var kidControl = new KidInfoControl(_parent, cities)
                {
                    Margin = new Thickness(15)
                };
                kidControl.KidSaved += OnKidSaved;
                KidsContainer.Children.Add(kidControl);
            }
        }

        private void OnKidSaved()
        {
            savedKidsCount++;
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void FinishRegistration(object sender, RoutedEventArgs e)
        {
            if (savedKidsCount > 0)
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            else
                ErrorText.Visibility = Visibility.Visible;
        }
    }
}
