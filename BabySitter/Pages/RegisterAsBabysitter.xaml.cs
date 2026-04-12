using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            
            dateofbirth.DisplayDateEnd = DateTime.Today;
            dateofbirth.DisplayDateStart = new DateTime(1940, 1, 1);
        }

        public async void PutCityData()
        {
            cities = await api.GetAllCitiesAsync();
            List<string> clist = new List<string>();
            foreach (City c in cities)
                clist.Add(c.CityName);
            cityname.ItemsSource = clist;
        }

        // ─── רק ספרות בטלפון ──────────────────────────────────────────────────────
        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // ─── רק ספרות במחיר ───────────────────────────────────────────────────────
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        // ─── בדיקת אימייל כשיוצאים מהשדה ────────────────────────────────────────
        private void RecommenderMail_LostFocus(object sender, RoutedEventArgs e)
        {
            string mail = recommenderMail.Text.Trim();
            if (!string.IsNullOrEmpty(mail) && !IsValidEmail(mail))
            {
                recommenderMail.Background = System.Windows.Media.Brushes.LightCoral;
                recommenderMail.ToolTip = "כתובת אימייל לא תקינה";
            }
            else
            {
                recommenderMail.ClearValue(TextBox.BackgroundProperty);
                recommenderMail.ToolTip = null;
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^05\d{8}$");
        }

        // ─── יצירת חשבון ──────────────────────────────────────────────────────────
        private async void CreateAccount(object sender, RoutedEventArgs e)
        {
            // 1. שם
            if (string.IsNullOrWhiteSpace(fname.Text) || string.IsNullOrWhiteSpace(lname.Text))
            {
                MessageBox.Show("נא למלא שם פרטי ושם משפחה");
                return;
            }

            // 2. תאריך לידה — DatePicker מחזיר null אם לא נבחר תאריך
            if (dateofbirth.SelectedDate == null)
            {
                MessageBox.Show("נא לבחור תאריך לידה");
                return;
            }

            DateTime dob = dateofbirth.SelectedDate.Value;
            int age = DateTime.Today.Year - dob.Year;
            if (dob > DateTime.Today.AddYears(-age)) age--;

            if (age < 14 || age > 80)
            {
                MessageBox.Show("גיל הבייביסיטר צריך להיות בין 14 ל-80");
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
                MessageBox.Show("מספר טלפון לא תקין.\nדוגמה: 0501234567");
                return;
            }
            int telephone = int.Parse(phoneText);

            // 5. מחיר
            if (!int.TryParse(price.Text, out int pricePerHour) || pricePerHour <= 0)
            {
                MessageBox.Show("נא להזין מחיר תקין לשעה");
                return;
            }

            // 6. אימייל ממליץ (אופציונלי)
            string mail = recommenderMail.Text.Trim();
            if (!string.IsNullOrEmpty(mail) && !IsValidEmail(mail))
            {
                MessageBox.Show("כתובת האימייל של הממליץ אינה תקינה");
                recommenderMail.Focus();
                return;
            }

            // 7. ✅ סיסמה — עכשיו קורא ישירות מה-PasswordBox האמיתי
            if (string.IsNullOrWhiteSpace(pass.Password))
            {
                MessageBox.Show("נא להזין סיסמה");
                return;
            }

            if (pass.Password != confirmpass.Password)
            {
                MessageBox.Show("הסיסמאות לא תואמות");
                return;
            }

            // ─── שליחה לשרת ───────────────────────────────────────────────────────
            City selectedCity = cities[cityname.SelectedIndex];

            BabySitterTeens teen = new BabySitterTeens()
            {
                FirstName = fname.Text.Trim(),
                LastName = lname.Text.Trim(),
                DateOfBirth = dob,
                PriceForAnHour = pricePerHour,
                MailOfRecommender = mail,
                Password = pass.Password,
                Telephone = telephone,
                CityNameId = selectedCity,
                ProfilePicture = " "
            };

            int result = await api.InsertBabySitterTeenAsync(teen);

            if (result > 0)
            {
                LogInComputer.LastRegisteredPhone = telephone;
                MessageBox.Show("הבייביסיטר נרשם בהצלחה! 🎉");
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("שגיאה בהרשמה, נסו שנית");
            }
        }

        private void LogIn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }
    }
}