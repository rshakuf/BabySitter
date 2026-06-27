using BabySitter.Helpers;
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
        private readonly Parents    _parent;
        private readonly ApiService _api = new ApiService();
        private int        _loadedKidsCount = 0;
        private List<City> _cities          = new List<City>();

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
                _cities = await _api.GetAllCitiesAsync();
                var allKids = await _api.GetAllChildrenOfParentsAsync();
                var myKids  = allKids?
                    .Where(c => c.IdParent?.Id == _parent.Id)
                    .ToList() ?? new List<ChildOfParent>();
                _loadedKidsCount = myKids.Count;
                CreateKidControls(_cities, myKids);
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה בטעינת ילדים: " + ex.Message, Window.GetWindow(this));
            }
        }

        private void CreateKidControls(List<City> cities, List<ChildOfParent> existingKids)
        {
            KidsContainer.Children.Clear();

            foreach (var kid in existingKids)
                KidsContainer.Children.Add(new KidInfoControl(_parent, cities, kid) { Margin = new Thickness(15) });

            int remaining = Math.Max((_parent.NumOfKids > 0 ? _parent.NumOfKids : 1) - existingKids.Count, 0);
            for (int i = 0; i < remaining; i++)
                KidsContainer.Children.Add(new KidInfoControl(_parent, cities) { Margin = new Thickness(15) });
        }

        private void AddKid_Click(object sender, RoutedEventArgs e)
        {
            KidsContainer.Children.Add(new KidInfoControl(_parent, _cities) { Margin = new Thickness(15) });
        }

        private async void FinishRegistration(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            FinishBtn.IsEnabled  = false;

            int  savedCount = 0;
            bool hasError   = false;

            foreach (UIElement el in KidsContainer.Children)
            {
                if (el is not KidInfoControl card) continue;

                // Track how many kids this card actually saves
                Action onSaved = () => savedCount++;
                card.KidSaved += onSaved;
                bool ok = await card.SaveAsync();
                card.KidSaved -= onSaved;

                if (!ok) { hasError = true; break; }
            }

            FinishBtn.IsEnabled = true;
            if (hasError) return; // per-card error already shown

            if (savedCount == 0 && _loadedKidsCount == 0)
            {
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }
    }
}
