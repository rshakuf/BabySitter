using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace BabySitter.Pages
{
    public partial class RegisterAsBabysitter : Page
    {
        List<City> cities;
        ApiService api = new ApiService();

        public RegisterAsBabysitter()
        {
            InitializeComponent();
            PutCityData();
        }

        public async void PutCityData()
        {
            cities = await api.GetAllCitiesAsync();
            List<string> clist = new List<string>();

            foreach (City c in cities)
                clist.Add(c.CityName);

            cityname.ItemsSource = clist;
        }

        private void LogIn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }

        private async void CreateAccount(object sender, RoutedEventArgs e)
        {
            if (pass.Password != confirmpass.Password)
            {
                MessageBox.Show("הסיסמאות לא תואמות");
                return;
            }

            if (cityname.SelectedIndex < 0)
            {
                MessageBox.Show("בחרי עיר");
                return;
            }

            if (!int.TryParse(phone.Text, out int telephone))
            {
                MessageBox.Show("מספר טלפון לא תקין");
                return;
            }

            DateTime.TryParse(dateofbirth.Text, out DateTime dob);
            int.TryParse(price.Text, out int pricePerHour);

            City selectedCity = cities[cityname.SelectedIndex];

            BabySitterTeens teen = new BabySitterTeens()
            {
                FirstName = fname.Text,
                LastName = lname.Text,
                DateOfBirth = dob,
                PriceForAnHour = pricePerHour,
                MailOfRecommender = recommenderMail.Text,
                Password = pass.Password,
                Telephone = telephone,
                CityNameId = selectedCity,
                ProfilePicture = " "
            };

            int result = await api.InsertBabySitterTeenAsync(teen);

            if (result > 0)
            {
                LogInComputer.LastRegisteredPhone = telephone;
                MessageBox.Show("הבייביסיטר נרשם בהצלחה");
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("שגיאה בהרשמה");
            }
        }
    }
}