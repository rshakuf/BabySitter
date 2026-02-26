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
    public partial class Register : Page
    {
        List<City> cities;
        ApiService api = new ApiService();

        public Register()
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
            string fn = fname.Text;
            string ln = lname.Text;
            string num = numofkids.Text;
            string pas = pass.Password;
            string confirmpasss = confirmpass.Password;

            City currcity = cities[cityname.SelectedIndex];

            if (confirmpasss != pas)
            {
                passerror.Visibility = Visibility.Visible;
                return;
            }

            DateTime dob;
            DateTime.TryParse(dateofbirth.Text, out dob);

            if (!int.TryParse(phone.Text, out int phoneNumber))
            {
                MessageBox.Show("מספר טלפון לא תקין");
                return;
            }
            Parents p = new Parents()
            {
                FirstName = fn,
                LastName = ln,
                CityNameId = currcity,
                NumOfKids = int.Parse(num),
                Password = pas,
                DateOfBirth = dob,
                Telephone = phoneNumber
            };

            int result = await api.InsertParentAsync(p);

            if (result > 0)
            {
                // ⭐ כאן נשמר הטלפון ללוגין
                LogInComputer.LastRegisteredPhone = p.Telephone;

                MessageBox.Show("ההורה נרשם בהצלחה");

                NavigationService.Navigate(new ChildOfParents(p));
            }
            else
            {
                MessageBox.Show("שגיאה בהרשמה");
            }
        }
    }
}