using BabySitter.Helpers;
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
        private void Mail_LostFocus(object sender, RoutedEventArgs e)
        {
            string value = recommenderMail.Text.Trim();
            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
            {
                recommenderMail.Background = System.Windows.Media.Brushes.LightCoral;
                recommenderMail.ToolTip    = "כתובת אימייל לא תקינה";
            }
            else
            {
                recommenderMail.ClearValue(TextBox.BackgroundProperty);
                recommenderMail.ToolTip = null;
            }
        }

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$");

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
                CustomDialogHelper.ShowWarning("נא למלא שם פרטי ושם משפחה", Window.GetWindow(this));
                return;
            }

            // 2. תאריך לידה — DatePicker מחזיר null אם לא נבחר תאריך
            if (dateofbirth.SelectedDate == null)
            {
                CustomDialogHelper.ShowWarning("נא לבחור תאריך לידה", Window.GetWindow(this));
                return;
            }

            DateTime dob = dateofbirth.SelectedDate.Value;
            int age = DateTime.Today.Year - dob.Year;
            if (dob > DateTime.Today.AddYears(-age)) age--;

            if (age < 14 || age > 80)
            {
                CustomDialogHelper.ShowWarning("גיל הבייביסיטר צריך להיות בין 14 ל-80", Window.GetWindow(this));
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
                CustomDialogHelper.ShowWarning("מספר טלפון לא תקין.\nדוגמה: 0501234567", Window.GetWindow(this));
                return;
            }
            // 5. מחיר
            if (!int.TryParse(price.Text, out int pricePerHour) || pricePerHour <= 0)
            {
                CustomDialogHelper.ShowWarning("נא להזין מחיר תקין לשעה", Window.GetWindow(this));
                return;
            }

            if (pricePerHour <= PriceWarningHelper.LowPriceThreshold &&
                !PriceWarningHelper.ConfirmLowPrice(pricePerHour, Window.GetWindow(this)))
                return;

            if (pricePerHour >= PriceWarningHelper.HighPriceThreshold &&
                !PriceWarningHelper.ConfirmHighPrice(pricePerHour, Window.GetWindow(this)))
                return;

            // 6. מייל — חובה + תקינות
            string mail = recommenderMail.Text.Trim();
            if (string.IsNullOrEmpty(mail))
            {
                CustomDialogHelper.ShowWarning("נא להזין כתובת מייל", Window.GetWindow(this));
                recommenderMail.Focus();
                return;
            }
            if (!IsValidEmail(mail))
            {
                CustomDialogHelper.ShowWarning("כתובת המייל אינה תקינה\nדוגמה: name@example.com", Window.GetWindow(this));
                recommenderMail.Focus();
                return;
            }

            // 7. ✅ סיסמה — עכשיו קורא ישירות מה-PasswordBox האמיתי
            if (string.IsNullOrWhiteSpace(pass.Password))
            {
                CustomDialogHelper.ShowWarning("נא להזין סיסמה", Window.GetWindow(this));
                return;
            }

            if (pass.Password != confirmpass.Password)
            {
                CustomDialogHelper.ShowWarning("הסיסמאות לא תואמות", Window.GetWindow(this));
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
                 Mail = mail,
                Password = pass.Password,
                Telephone = phoneText,
                CityNameId = selectedCity,
            };

            int result = await api.InsertBabySitterTeenAsync(teen);

            if (result > 0)
            {
                LogInComputer.LastRegisteredPhone = phoneText;
                CustomDialogHelper.ShowSuccess("הבייביסיטר נרשם בהצלחה! 🎉", Window.GetWindow(this));
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            }
            else
            {
                CustomDialogHelper.ShowError("שגיאה בהרשמה, נסו שנית", Window.GetWindow(this));
            }
        }

        private void LogIn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }

    }
}