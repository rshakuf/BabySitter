using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClApi;
using Model;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BabySitter.Pages
{
    public partial class MyProfile : Page
    {
        ApiService api = new ApiService();
        private bool isPasswordVisible = false;

        // 🔥 רשימת ילדים אמיתית מהשרת
        private List<ChildOfParent> ChildOfParentList = new List<ChildOfParent>();

        public MyProfile()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            // 1. Fetch all cities from the DB to populate the ComboBox
            // *Change 'GetAllCitiesAsync()' to match your actual ApiService method*
            var allCities = await api.GetAllCitiesAsync();
            cityComboBox.ItemsSource = allCities;

            if (LogInComputer.WhoAmI == "babysitter")
            {
                var user = (BabySitterTeens)LogInComputer.CurrentUser;

                fname.Text = user.FirstName;
                lname.Text = user.LastName;
                phone.Text = user.Telephone.ToString();

                // Select the user's current city in the ComboBox
                if (user.CityNameId != null)
                {
                    cityComboBox.SelectedItem = allCities.FirstOrDefault(c => c.Id == user.CityNameId.Id);
                }

                pass.Password = user.Password;
                passVisible.Text = user.Password;
            }

            else if (LogInComputer.WhoAmI == "parent")
            {
                var user = (Parents)LogInComputer.CurrentUser;

                fname.Text = user.FirstName;
                lname.Text = user.LastName;
                phone.Text = user.Telephone.ToString();

                // Select the user's current city in the ComboBox
                if (user.CityNameId != null)
                {
                    cityComboBox.SelectedItem = allCities.FirstOrDefault(c => c.Id == user.CityNameId.Id);
                }

                pass.Password = user.Password;
                passVisible.Text = user.Password;

                // שליפת כל הילדים
                ChildOfParentList = await api.GetAllChildOfParentsAsync();

                // סינון לפי ההורה המחובר
                ChildOfParentList = ChildOfParentList
                    .Where(x => x.IdParent.Id == user.Id)
                    .ToList();

                foreach (var child in ChildOfParentList)
                {
                    StackPanel panel = new StackPanel
                    {
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    panel.Children.Add(new TextBlock
                    {
                        Text = "שם ילד"
                    });

                    TextBox tb = new TextBox
                    {
                        Text = child.FirstName,
                        Tag = child
                    };

                    panel.Children.Add(tb);
                    KidsPanel.Children.Add(panel);
                }
            }
        }

        // ─── הצגת סיסמה ─────────────────────
        private void TogglePassword(object sender, MouseButtonEventArgs e)
        {
            if (isPasswordVisible)
            {
                pass.Visibility = Visibility.Visible;
                passVisible.Visibility = Visibility.Collapsed;
            }
            else
            {
                pass.Visibility = Visibility.Collapsed;
                passVisible.Visibility = Visibility.Visible;
            }

            isPasswordVisible = !isPasswordVisible;
        }

        // ─── רק ספרות ─────────────────────
        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^05\d{8}$");
        }

        // ─── שמירה ─────────────────────
        private async void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fname.Text) || string.IsNullOrWhiteSpace(lname.Text))
            {
                MessageBox.Show("נא למלא שם");
                return;
            }

            if (!IsValidPhone(phone.Text))
            {
                MessageBox.Show("טלפון לא תקין");
                return;
            }

            if (cityComboBox.SelectedItem == null)
            {
                MessageBox.Show("נא לבחור עיר");
                return;
            }

            int phoneNumber = int.Parse(phone.Text);

            SaveBtn.IsEnabled = false;

            // ─── בייביסיטר ─────────────────
            if (LogInComputer.WhoAmI == "babysitter")
            {
                var user = (BabySitterTeens)LogInComputer.CurrentUser;

                user.FirstName = fname.Text;
                user.LastName = lname.Text;
                user.Telephone = phoneNumber;

                // Update the city object reference dynamically from the ComboBox
                user.CityNameId = (City)cityComboBox.SelectedItem;
                int result = await api.UpdateBabySitterTeenAsync(user);

                MessageBox.Show(result > 0 ? "נשמר!" : "שגיאה");
            }

            // ─── הורה ─────────────────
            else if (LogInComputer.WhoAmI == "parent")
            {
                var user = (Parents)LogInComputer.CurrentUser;

                user.FirstName = fname.Text;
                user.LastName = lname.Text;
                user.Telephone = phoneNumber;

                // Update the city object reference dynamically from the ComboBox
                user.CityNameId = (City)cityComboBox.SelectedItem;
                int result = await api.UpdateParentAsync(user);

                if (result <= 0)
                {
                    MessageBox.Show("שגיאה");
                    SaveBtn.IsEnabled = true;
                    return;
                }

                // 🔥 עדכון ילדים
                foreach (StackPanel panel in KidsPanel.Children)
                {
                    TextBox tb = panel.Children[1] as TextBox;
                    var child = (ChildOfParent)tb.Tag;

                    child.FirstName = tb.Text;

                    await api.UpdateChildOfParentAsync(child);
                }

                MessageBox.Show("נשמר!");
            }

            SaveBtn.IsEnabled = true;
        }
    }
}