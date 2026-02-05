using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ApiInterface;
using ClApi;
using Model;

namespace BabySitter.Pages
{
    /// <summary>
    /// Interaction logic for LogInComputer.xaml
    /// </summary>
    public partial class LogInComputer : Page
    {
        private ApiService apiService = new ApiService();

        public LogInComputer()
        {
            InitializeComponent();
        }

        private void ParentRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/Register.xaml", UriKind.Relative));
        }

        private void BabysitterRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/RegisterAsBabysitter.xaml", UriKind.Relative));
        }

        private async void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            int tel =int.Parse( userNameTextBox.Text);
            string password = PasswordBox.Password;
            ParentsList pList =await  apiService.GetAllParentsAsync();
            Parents p1 =pList.Find(x=>x.Telephone== tel && x.== password);  
            if(p1==null)

            if (await ValidateCredentials(username, password))
            {
                NavigationService?.Navigate(new Uri("Pages/Home.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show("Invalid username or password", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<bool> ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // TODO: Replace with your actual API login method
                // Example: var user = await apiService.LoginAsync(username, password);
                // return user != null;

                // Placeholder - replace with actual API call
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    }

