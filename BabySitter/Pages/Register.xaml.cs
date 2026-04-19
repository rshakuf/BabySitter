using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        // ─── רק ספרות בטלפון ─────────────────────────────────
        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // ─── רק ספרות במספר ילדים ─────────────────────────────
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^05\d{8}$");
        }

        private async void CreateAccount(object sender, RoutedEventArgs e)
        {
            // 1. שם
            if (string.IsNullOrWhiteSpace(fname.Text) || string.IsNullOrWhiteSpace(lname.Text))
            {
                MessageBox.Show("נא למלא שם פרטי ושם משפחה");
                return;
            }

            // 2. תאריך לידה
            if (dateofbirth.SelectedDate == null)
            {
                MessageBox.Show("נא לבחור תאריך לידה");
                return;
            }

            DateTime dob = dateofbirth.SelectedDate.Value;
            int age = DateTime.Today.Year - dob.Year;
            if (dob > DateTime.Today.AddYears(-age)) age--;

            if (age < 18 || age > 120)
            {
                MessageBox.Show("גיל ההורה צריך להיות בין 18 ל-120");
                return;
            }

            // 3. עיר
            if (cityname.SelectedIndex < 0)
            {
                MessageBox.Show("נא לבחור עיר");
                return;
            }

            // 4. טלפון
            string phoneText = phone.Text.Trim();
            if (!IsValidPhone(phoneText))
            {
                MessageBox.Show("מספר טלפון לא תקין\nדוגמה: 0501234567");
                return;
            }
            int phoneNumber = int.Parse(phoneText);

            // 5. מספר ילדים
            if (!int.TryParse(numofkids.Text, out int kids) || kids <= 0)
            {
                MessageBox.Show("נא להזין מספר ילדים תקין");
                return;
            }

            // 6. סיסמה
            if (string.IsNullOrWhiteSpace(pass.Password))
            {
                MessageBox.Show("נא להזין סיסמה");
                return;
            }

            if (pass.Password != confirmpass.Password)
            {
                passerror.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                passerror.Visibility = Visibility.Collapsed;
            }

            // יצירת אובייקט
            City currcity = cities[cityname.SelectedIndex];

            Parents p = new Parents()
            {
                FirstName = fname.Text.Trim(),
                LastName = lname.Text.Trim(),
                CityNameId = currcity,
                NumOfKids = kids,
                Password = pass.Password,
                DateOfBirth = dob,
                Telephone = phoneNumber,
                 
                
            };

            int result = await api.InsertParentAsync(p);

            if (result > 0)
            {
                LogInComputer.LastRegisteredPhone = p.Telephone;

                MessageBox.Show("ההורה נרשם בהצלחה 🎉");

                NavigationService.Navigate(new ChildOfParents(p));
            }
            else
            {
                MessageBox.Show("שגיאה בהרשמה");
            }
           
        }
    }
}