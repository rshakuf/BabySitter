using System;
using System.Windows;
using System.Windows.Controls;
using Model;

namespace BabySitter.Pages
{
    public partial class ChildOfParents : Page
    {
        private Parents parent;
        private int savedKidsCount = 0;

        public ChildOfParents(Parents p)
        {
            InitializeComponent();
            parent = p;
            CreateKidControls();
        }

        private void CreateKidControls()
        {
            KidsContainer.Children.Clear();

            for (int i = 0; i < parent.NumOfKids; i++)
            {
                KidInfoControl kidControl = new KidInfoControl(parent);
                kidControl.Margin = new Thickness(15);

                kidControl.KidSaved += OnKidSaved;

                KidsContainer.Children.Add(kidControl);
            }
        }

        private void OnKidSaved()
        {
            savedKidsCount++;
            ErrorText.Visibility = Visibility.Collapsed;
        }

        private void FinishRegistration(object sender, RoutedEventArgs e)
        {
            if (savedKidsCount > 0)
            {
                NavigationService.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}