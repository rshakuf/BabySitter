using BabySitter.Helpers;
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
            try
            {
                cities = await api.GetAllCitiesAsync();
                List<string> clist = new List<string>();
                foreach (City c in cities)
                    clist.Add(c.CityName);
                cityname.ItemsSource = clist;
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה בטעינת ערים — ודאי שהשרת פועל.\n" + ex.Message, Window.GetWindow(this));
            }
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
                CustomDialogHelper.ShowWarning("נא למלא שם פרטי ושם משפחה", Window.GetWindow(this));
                return;
            }

            // 2. תאריך לידה
            if (dateofbirth.SelectedDate == null)
            {
                CustomDialogHelper.ShowWarning("נא לבחור תאריך לידה", Window.GetWindow(this));
                return;
            }

            DateTime dob = dateofbirth.SelectedDate.Value;
            int age = DateTime.Today.Year - dob.Year;
            if (dob > DateTime.Today.AddYears(-age)) age--;

            if (age < 18 || age > 120)
            {
                CustomDialogHelper.ShowWarning("גיל ההורה צריך להיות בין 18 ל-120", Window.GetWindow(this));
                return;
            }

            // 3. עיר
            if (cityname.SelectedIndex < 0)
            {
                CustomDialogHelper.ShowWarning("נא לבחור עיר", Window.GetWindow(this));
                return;
            }

            // 4. טלפון
            string phoneText = phone.Text.Trim();
            if (!IsValidPhone(phoneText))
            {
                CustomDialogHelper.ShowWarning("מספר טלפון לא תקין\nדוגמה: 0501234567", Window.GetWindow(this));
                return;
            }

            // 5. מספר ילדים
            if (!int.TryParse(numofkids.Text, out int kids) || kids <= 0)
            {
                CustomDialogHelper.ShowWarning("נא להזין מספר ילדים תקין", Window.GetWindow(this));
                return;
            }

            // 6. סיסמה
            if (string.IsNullOrWhiteSpace(pass.Password))
            {
                CustomDialogHelper.ShowWarning("נא להזין סיסמה", Window.GetWindow(this));
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
                Telephone = phoneText,
                 
                
            };

            int result = await api.InsertParentAsync(p);

            if (result > 0)
            {
                LogInComputer.LastRegisteredPhone = p.Telephone;

                // Fetch all parents to find the real record with the DB-assigned Id.
                // (The local 'p' object still has Id=0 because @@IDENTITY only runs on the server.)
                var allParents = await api.GetAllParentsAsync();
                var realParent = allParents?.FirstOrDefault(x => x.Telephone == p.Telephone) ?? p;

                CustomDialogHelper.ShowSuccess("ההורה נרשם בהצלחה! 🎉", Window.GetWindow(this));
                NavigationService.Navigate(new ChildOfParents(realParent));
            }
            else
            {
                CustomDialogHelper.ShowError("שגיאה בהרשמה", Window.GetWindow(this));
            }
           
        }
    }
}