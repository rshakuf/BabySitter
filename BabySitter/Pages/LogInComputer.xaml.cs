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

        public LogInComputer()
        {
            InitializeComponent();
        }

        private void AutoFillButton_Click(object sender, RoutedEventArgs e)
        {
            userNameTextBox.Text = "1528040991";
            PasswordBox.Password = "1234";
            LogInButton_Click(null, null);
        }

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
                // 🔹 CHECK PARENTS
                ParentsList pList = await apiService.GetAllParentsAsync();

                Parents parent =
                    pList?.Find(x => x.Telephone == tel && x.Password.Trim() == password);

                if (parent != null)
                {
                    MessageBox.Show($"ברוך הבא {parent.FirstName}");
                    NavigationService.Navigate(new Uri("Pages/Home.xaml", UriKind.Relative));
                    return;
                }

                // 🔹 CHECK BABYSITTERS
                BabySitterTeensList bstList = await apiService.GetAllBabySitterTeensAsync();

                BabySitterTeens bst =
                    bstList?.Find(x => x.Telephone == tel && x.Password.Trim() == password);

                if (bst != null)
                {
                    MessageBox.Show($"ברוכה הבאה {bst.FirstName}");
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

        private async Task<bool> ValidateCredentials(int tel, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                ParentsList pList = await apiService.GetAllParentsAsync();
                if (pList?.Find(x => x.Telephone == tel && x.Password == password) != null)
                    return true;

                BabySitterTeensList bstList = await apiService.GetAllBabySitterTeensAsync();
                if (bstList?.Find(x => x.Telephone == tel && x.Password == password) != null)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
