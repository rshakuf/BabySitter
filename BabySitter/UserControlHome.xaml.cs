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

            var nav = NavigationService.GetNavigationService(this);
            if (nav != null)
            {
                // Parents get the interactive availability page; others get the read-only detail view
                if (LogInComputer.WhoAmI == "parent")
                    nav.Navigate(new AvailabilityPage(teen));
                else
                    nav.Navigate(new BabySitterDetailsControl(teen));
            }
        }
    }
}
