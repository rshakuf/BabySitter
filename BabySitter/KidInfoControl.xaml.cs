using ClApi;
using Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BabySitter
{
    public partial class KidInfoControl : UserControl
    {
        private readonly Parents _parent;
        private readonly ApiService _api = new ApiService();
        private ChildOfParent _existingChild; // null = new child

        public event System.Action KidSaved;

        // ── Public accessors (kept for backward compatibility) ─────────────────
        public TextBox    FirstNameTextBoxPublic  => FirstNameTextBox;
        public TextBox    LastNameTextBoxPublic   => LastNameTextBox;
        public DatePicker BirthDatePickerPublic   => BirthDatePicker;
        public ComboBox   CityComboBoxPublic      => CityComboBox;

        /// <summary>
        /// Create a card for an EXISTING child (edit mode).
        /// Pass hideSaveButton=true when inside MyProfile (global save button handles it).
        /// </summary>
        public KidInfoControl(Parents parent, List<City> cities, ChildOfParent existingChild, bool hideSaveButton = false)
        {
            InitializeComponent();
            _parent        = parent;
            _existingChild = existingChild;

            CityComboBox.ItemsSource = cities;

            // Pre-fill all fields from the existing child
            CardTitle.Text               = $"ילד: {existingChild.FirstName}";
            FirstNameTextBox.Text        = existingChild.FirstName  ?? "";
            LastNameTextBox.Text         = existingChild.LastName   ?? "";
            BirthDatePicker.SelectedDate = existingChild.DateOfBirth;
            CityComboBox.SelectedItem    = cities.Find(c => c.Id == existingChild.CityNameId?.Id);
            SaveBtn.Content              = "עדכן";

            if (hideSaveButton) SaveBtn.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Create a card for a NEW child (add mode).
        /// Pass hideSaveButton=true when inside MyProfile (global save button handles it).
        /// </summary>
        public KidInfoControl(Parents parent, List<City> cities, bool hideSaveButton = false)
        {
            InitializeComponent();
            _parent        = parent;
            _existingChild = null;

            CityComboBox.ItemsSource = cities;

            // Sensible defaults for a new child
            LastNameTextBox.Text      = parent.LastName ?? "";
            CityComboBox.SelectedItem = cities.Find(c => c.Id == parent.CityNameId?.Id);

            if (hideSaveButton) SaveBtn.Visibility = Visibility.Collapsed;
        }

        // ── Save / Update ──────────────────────────────────────────────────────

        /// <summary>
        /// Called by MyProfile's global "שמור שינויים" button.
        /// Returns true on success, false on validation error.
        /// Silently skips completely empty new-child cards.
        /// </summary>
        public async Task<bool> SaveAsync()
        {
            // Skip blank new-child cards the user hasn't touched
            bool isEmpty = string.IsNullOrWhiteSpace(FirstNameTextBox.Text) &&
                           string.IsNullOrWhiteSpace(LastNameTextBox.Text)  &&
                           BirthDatePicker.SelectedDate == null;
            if (_existingChild == null && isEmpty) return true;

            ErrorText.Visibility = Visibility.Collapsed;
            SavedText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text)  ||
                BirthDatePicker.SelectedDate == null             ||
                CityComboBox.SelectedItem    == null)
            {
                ErrorText.Text       = "נא למלא את כל השדות";
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            var selectedCity = (City)CityComboBox.SelectedItem;

            if (_existingChild != null)
            {
                _existingChild.FirstName   = FirstNameTextBox.Text.Trim();
                _existingChild.LastName    = LastNameTextBox.Text.Trim();
                _existingChild.DateOfBirth = BirthDatePicker.SelectedDate.Value;
                _existingChild.CityNameId  = selectedCity;
                await _api.UpdateChildOfParentAsync(_existingChild);
                CardTitle.Text = $"ילד: {_existingChild.FirstName}";
            }
            else
            {
                var child = new ChildOfParent
                {
                    FirstName   = FirstNameTextBox.Text.Trim(),
                    LastName    = LastNameTextBox.Text.Trim(),
                    DateOfBirth = BirthDatePicker.SelectedDate.Value,
                    IdParent    = _parent,
                    CityNameId  = selectedCity
                };
                await _api.InsertChildOfParentAsync(child);
                _existingChild  = child;
                SaveBtn.Content = "עדכן";
                CardTitle.Text  = $"ילד: {child.FirstName}";
            }

            SavedText.Visibility = Visibility.Visible;
            KidSaved?.Invoke();
            return true;
        }

        private async void SaveKid_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            SavedText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text)  ||
                BirthDatePicker.SelectedDate == null             ||
                CityComboBox.SelectedItem == null)
            {
                ErrorText.Text       = "נא למלא את כל השדות";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            SaveBtn.IsEnabled = false;

            var selectedCity = (City)CityComboBox.SelectedItem;

            if (_existingChild != null)
            {
                // ── UPDATE existing child ──────────────────────────────────────
                _existingChild.FirstName   = FirstNameTextBox.Text.Trim();
                _existingChild.LastName    = LastNameTextBox.Text.Trim();
                _existingChild.DateOfBirth = BirthDatePicker.SelectedDate.Value;
                _existingChild.CityNameId  = selectedCity;

                await _api.UpdateChildOfParentAsync(_existingChild);
                CardTitle.Text = $"ילד: {_existingChild.FirstName}";
            }
            else
            {
                // ── INSERT new child ──────────────────────────────────────────
                var child = new ChildOfParent
                {
                    FirstName   = FirstNameTextBox.Text.Trim(),
                    LastName    = LastNameTextBox.Text.Trim(),
                    DateOfBirth = BirthDatePicker.SelectedDate.Value,
                    IdParent    = _parent,
                    CityNameId  = selectedCity
                };

                await _api.InsertChildOfParentAsync(child);

                // Switch to edit mode so next save is an update
                _existingChild  = child;
                SaveBtn.Content = "עדכן";
                CardTitle.Text  = $"ילד: {child.FirstName}";
            }

            SavedText.Visibility = Visibility.Visible;
            SaveBtn.IsEnabled    = true;
            KidSaved?.Invoke();
        }
    }
}
