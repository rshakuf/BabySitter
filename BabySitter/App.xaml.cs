using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;

namespace BabySitter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var window = new System.Windows.Navigation.NavigationWindow
            {
                Width = 1100,
                Height = 950,
                Title = "BabySitter",
                ShowsNavigationUI = true
            };
            window.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            window.Show();
        }
    }

}
