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

namespace BabySitter
{
    /// <summary>
    /// Interaction logic for ChildOfParentsUserControl.xaml
    /// </summary>
    public partial class ChildOfParentsUserControl : UserControl
    {
        Parents parents;
        ApiService apiService = new ApiService();   
        public ChildOfParentsUserControl(Parents  par)
        {
            InitializeComponent();
            parents= par;
        }

        private void AddChild(object sender, RoutedEventArgs e)
        {
            string childName = ChildNameTextBox.Text;

            //ChildOfParent childOfParent = new ChildOfParent() {IdParent = Parents.ID, FirstName = childName }; 

            //apiService.InsertChildOfParentAsync(childOfParent);

        }
    }
}
