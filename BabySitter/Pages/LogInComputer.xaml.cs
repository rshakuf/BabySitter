using ApiInterface;
using ClApi;
using Model;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BabySitter.Pages
{
    public partial class LogInComputer : Page
    {
        private ApiService apiService = new ApiService();

        public static User CurrentUser = null;
        public static string WhoAmI = null;

        // ⭐ נשמר כאן הטלפון אחרי הרשמה
        public static int? LastRegisteredPhone { get; set; }

        public LogInComputer()
        {
            InitializeComponent();
            Loaded += LogInComputer_Loaded;
        }

        // ⭐ ממלא טלפון אוטומטית אחרי הרשמה
        private void LogInComputer_Loaded(object sender, RoutedEventArgs e)
        {
            if (LastRegisteredPhone.HasValue)
            {
                userNameTextBox.Text = LastRegisteredPhone.Value.ToString();
                PasswordBox.Focus();
            }
        }

        // ⭐ כפתור מילוי אוטומטי הורה
        private void AutoFillButton_Click(object sender, RoutedEventArgs e)
        {
            userNameTextBox.Text = "1528040991";
            PasswordBox.Password = "1234";
            LogInButton_Click(null, null);
        }

        // ⭐ כפתור מילוי אוטומטי בייביסיטר
        private void AutoFillBabysitterButton_Click(object sender, RoutedEventArgs e)
        {
            userNameTextBox.Text = "67676767";
            PasswordBox.Password = "12345";
            LogInButton_Click(null, null);
        }

        private void ParentRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Pages/Register.xaml", UriKind.Relative));
        }

        private void BabysitterRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("Pages/RegisterAsBabysitter.xaml", UriKind.Relative));
        }

        private async void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(userNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("הכנס טלפון וסיסמה");
                return;
            }

            if (!int.TryParse(userNameTextBox.Text, out int tel))
            {
                MessageBox.Show("מספר טלפון לא תקין");
                return;
            }

            string password = PasswordBox.Password;

            try
            {
                ParentsList pList = await apiService.GetAllParentsAsync();
                Parents parent =
                    pList?.Find(x => x.Telephone == tel && x.Password.Trim() == password);

                if (parent != null)
                {
                    CurrentUser = parent;
                    WhoAmI = "parent";
                    NavigationService.Navigate(new Uri("Pages/Home.xaml", UriKind.Relative));
                    return;
                }

                BabySitterTeensList bstList = await apiService.GetAllBabySitterTeensAsync();
                BabySitterTeens bst =
                    bstList?.Find(x => x.Telephone == tel && x.Password.Trim() == password);

                if (bst != null)
                {
                    CurrentUser = bst;
                    WhoAmI = "babysitter";
                    NavigationService.Navigate(new Uri("Pages/Home.xaml", UriKind.Relative));
                    return;
                }

                MessageBox.Show("טלפון או סיסמה שגויים");
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאת התחברות: " + ex.Message);
            }
        }
    }
}