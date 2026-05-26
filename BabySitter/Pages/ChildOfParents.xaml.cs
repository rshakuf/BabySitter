using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter.Pages
{
    public partial class ChildOfParents : Page
    {
        private readonly Parents _parent;
        private readonly ApiService _api = new ApiService();
        private int savedKidsCount  = 0;
        private int _loadedKidsCount = 0;

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
                var cities  = await _api.GetAllCitiesAsync();
                var allKids = await _api.GetAllChildrenOfParentsAsync();
                var myKids  = allKids?
                    .Where(c => c.IdParent?.Id == _parent.Id)
                    .ToList() ?? new List<ChildOfParent>();
                _loadedKidsCount = myKids.Count;
                CreateKidControls(cities, myKids);
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת ילדים: " + ex.Message);
            }
        }

        private void CreateKidControls(List<City> cities, List<ChildOfParent> existingKids)
        {
            KidsContainer.Children.Clear();

            // Show cards for kids already saved in the DB (edit mode)
            foreach (var kid in existingKids)
            {
                var kidControl = new KidInfoControl(_parent, cities, kid)
                {
                    Margin = new Thickness(15)
                };
                kidControl.KidSaved += OnKidSaved;
                KidsContainer.Children.Add(kidControl);
            }

            // Show empty cards for kids not yet entered
            int target    = _parent.NumOfKids > 0 ? _parent.NumOfKids : 1;
            int remaining = target - existingKids.Count;
            for (int i = 0; i < remaining; i++)
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
            // Allow finish if at least one kid was saved this session,
            // OR the parent already had kids stored in the DB (loaded on entry)
            if (savedKidsCount > 0 || _loadedKidsCount > 0)
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            else
                ErrorText.Visibility = Visibility.Visible;
        }
    }
}
