using System.Windows;
using System.Windows.Controls;
using Model;
using ClApi;
// Include your other usings as needed

namespace BabySitter.Pages
{
    /// <summary>
    /// Interaction logic for ChildOfParents.xaml
    /// </summary>
    public partial class ChildOfParents : Page
    {
        private Parents parent;

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

                // Changed margin to 15 on all sides so they space out beautifully in the WrapPanel
                kidControl.Margin = new Thickness(15);

                KidsContainer.Children.Add(kidControl);
            }
        }
    }
}