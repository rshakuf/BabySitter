using ClApi;
using Model;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter
{
    public partial class KidInfoControl : UserControl
    {
        private Parents parent;
        private ApiService api = new ApiService();
        private List<City> cities;

        public event Action KidSaved;
        public TextBox FirstNameTextBoxPublic => FirstNameTextBox;
        public TextBox LastNameTextBoxPublic => LastNameTextBox;
        public DatePicker BirthDatePickerPublic => BirthDatePicker;
        public ComboBox CityComboBoxPublic => CityComboBox;

        public KidInfoControl(Parents p)
        {
            InitializeComponent();
            parent = p;

            // שם משפחה אוטומטי מההורה
            LastNameTextBox.Text = parent.LastName;

            LoadCities();
        }

        private async void LoadCities()
        {
            cities = await api.GetAllCitiesAsync();

            List<string> names = new List<string>();
            foreach (City c in cities)
                names.Add(c.CityName);

            CityComboBox.ItemsSource = names;

            // בחירת העיר של ההורה כברירת מחדל
            if (parent.CityNameId != null)
                CityComboBox.SelectedItem = parent.CityNameId.CityName;
        }

        private async void SaveKid_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            SavedText.Visibility = Visibility.Collapsed;

            // בדיקת שדות ריקים
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                BirthDatePicker.SelectedDate == null ||
                CityComboBox.SelectedIndex == -1)
            {
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            ChildOfParent child = new ChildOfParent();

            child.FirstName = FirstNameTextBox.Text;
            child.LastName = LastNameTextBox.Text;
            child.DateOfBirth = BirthDatePicker.SelectedDate.Value;
            child.IdParent = parent;
            child.CityNameId = cities[CityComboBox.SelectedIndex];

            await api.InsertChildOfParentAsync(child);

            SavedText.Visibility = Visibility.Visible;

            KidSaved?.Invoke();
        }
    }
}