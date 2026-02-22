using ClApi;
using Model;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter
{
    public partial class KidInfoControl : UserControl
    {
        private Parents parent;

        // כמו שיש לך במסך יצירת הורה
        private ApiService api = new ApiService();

        public KidInfoControl(Parents p)
        {
            InitializeComponent();
            parent = p;
        }

        private void SaveKid_Click(object sender, RoutedEventArgs e)
        {
            ChildOfParent child = new ChildOfParent();

            child.FirstName = FirstNameTextBox.Text;
            child.LastName = LastNameTextBox.Text;

            if (BirthDatePicker.SelectedDate != null)
                child.DateOfBirth = BirthDatePicker.SelectedDate.Value;

            child.IdParent = parent; // קישור לילד להורה

            api.InsertChildOfParentAsync(child); // ← שמירה למסד כמו הורה

            MessageBox.Show("הילד נשמר בהצלחה");
        }
    }
}