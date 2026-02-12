using Model;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter.UserControls
{
    public partial class BabySitterDetailsControl : UserControl
    {
        public BabySitterDetailsControl(BabySitterTeens teen)
        {
            InitializeComponent();
            // This allows all the {Binding ...} calls in XAML to find the teen's data
            this.DataContext = teen;
        }

        // Optional: If you want the button inside the control to close the window
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}