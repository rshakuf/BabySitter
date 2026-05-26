using System.Windows.Controls;

namespace BabySitter.Pages
{
    public partial class AboutUs : Page
    {
        public AboutUs()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
