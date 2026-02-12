using Model;
using System.Windows;
using System.Windows.Controls;
using BabySitter.UserControls;

namespace BabySitter
{
    public partial class UserControlHome : UserControl
    {
        public UserControlHome()
        {
            InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var teen = button?.DataContext as BabySitterTeens;

            if (teen != null)
            {
                Window window = new Window
                {
                    Title = "פרטים נוספים",
                    Content = new BabySitterDetailsControl(teen),
                    Width = 650,
                    Height = 550,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                window.ShowDialog();
            }
        }
    }
}
