using BabySitter.Helpers;
using BabySitter.Pages;
using BabySitter.UserControls;
using Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BabySitter.UserControls
{
    public partial class UserControlHome : UserControl
    {
        public UserControlHome()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => ApplyAvatar();
            Loaded += (s, e) => ApplyAvatar();
        }

        private void ApplyAvatar()
        {
            if (DataContext is not BabySitterTeens teen) return;
            ImageHelper.ApplyAvatar(teen.ProfilePicture, teen.FirstName,
                CardAvatarLetter, CardAvatarImage, CardAvatarBrush);
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
