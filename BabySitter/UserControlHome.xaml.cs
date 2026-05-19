using Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BabySitter.UserControls;
using BabySitter.Pages;

namespace BabySitter.UserControls
{
    public partial class UserControlHome : UserControl
    {
        public UserControlHome()
        {
            InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            var teen = (sender as Button)?.DataContext as BabySitterTeens;
            if (teen == null) return;

            var control = new BabySitterDetailsControl(teen);
            var window = new Window
            {
                Title = "פרטים נוספים",
                Content = control,
                Width = 880,
                Height = 820,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize
            };

            window.ShowDialog();

            // After the dialog closes, navigate to RequestsParents if a request was sent
            if (control.RequestWasSent)
            {
                var nav = NavigationService.GetNavigationService(this);
                nav?.Navigate(new RequestsParents());
            }
        }
    }
}
