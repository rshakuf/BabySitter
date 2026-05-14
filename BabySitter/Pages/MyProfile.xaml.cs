using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClApi;
using Model;

namespace BabySitter.Pages
{
    public partial class MyProfile : Page
    {
        ApiService api = new ApiService();
        private bool isPasswordVisible = false;

        private List<ChildOfParent> ChildOfParentList = new List<ChildOfParent>();

        public MyProfile()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            var allCities = await api.GetAllCitiesAsync();
            cityComboBox.ItemsSource = allCities;

            if (LogInComputer.WhoAmI == "babysitter")
            {
                var user = (BabySitterTeens)LogInComputer.CurrentUser;

                fname.Text = user.FirstName;
                lname.Text = user.LastName;
                phone.Text = user.Telephone.ToString();

                cityComboBox.SelectedItem =
                    allCities.FirstOrDefault(c => c.Id == user.CityNameId?.Id);

                pass.Password = user.Password;
                passVisible.Text = user.Password;

                KidsSection.Visibility = Visibility.Collapsed;
            }

            else if (LogInComputer.WhoAmI == "parent")
            {
                var user = (Parents)LogInComputer.CurrentUser;

                fname.Text = user.FirstName;
                lname.Text = user.LastName;
                phone.Text = user.Telephone.ToString();

                cityComboBox.SelectedItem =
                    allCities.FirstOrDefault(c => c.Id == user.CityNameId?.Id);

                pass.Password = user.Password;
                passVisible.Text = user.Password;

                KidsSection.Visibility = Visibility.Visible;

                // ילדים מה־API
                ChildOfParentList = await api.GetAllChildrenOfParentsAsync();

                ChildOfParentList = ChildOfParentList
                    .Where(x => x.IdParent.Id == user.Id)
                    .ToList();

                KidsPanel.Children.Clear();

                int missingKids = user.NumOfKids - ChildOfParentList.Count;

                foreach (var child in ChildOfParentList)
                {
                    KidInfoControl control = new KidInfoControl(user);

                    control.FirstNameTextBoxPublic.Text = child.FirstName;
                    control.LastNameTextBoxPublic.Text = child.LastName;
                    control.BirthDatePickerPublic.SelectedDate = child.DateOfBirth;

                    control.CityComboBoxPublic.ItemsSource = allCities;
                    control.CityComboBoxPublic.DisplayMemberPath = "CityName";

                    control.CityComboBoxPublic.SelectedItem =
                        allCities.FirstOrDefault(c => c.Id == child.CityNameId?.Id);

                    control.Tag = child;

                    KidsPanel.Children.Add(control);
                }

                for (int i = 0; i < missingKids; i++)
                {
                    KidInfoControl emptyControl = new KidInfoControl(user);

                    emptyControl.CityComboBoxPublic.ItemsSource = allCities;
                    emptyControl.CityComboBoxPublic.DisplayMemberPath = "CityName";

                    KidsPanel.Children.Add(emptyControl);
                }
            }
        }

        private void TogglePassword(object sender, MouseButtonEventArgs e)
        {
            pass.Visibility = isPasswordVisible ? Visibility.Visible : Visibility.Collapsed;
            passVisible.Visibility = isPasswordVisible ? Visibility.Collapsed : Visibility.Visible;

            isPasswordVisible = !isPasswordVisible;
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^05\d{8}$");
        }

        private async void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fname.Text) ||
                string.IsNullOrWhiteSpace(lname.Text))
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

            if (LogInComputer.WhoAmI == "babysitter")
            {
                var user = (BabySitterTeens)LogInComputer.CurrentUser;

                user.FirstName = fname.Text;
                user.LastName = lname.Text;
                user.Telephone = phoneNumber;
                user.CityNameId = (City)cityComboBox.SelectedItem;

                int result = await api.UpdateBabySitterTeenAsync(user);
                MessageBox.Show(result > 0 ? "נשמר!" : "שגיאה");
            }

            else if (LogInComputer.WhoAmI == "parent")
            {
                var user = (Parents)LogInComputer.CurrentUser;

                user.FirstName = fname.Text;
                user.LastName = lname.Text;
                user.Telephone = phoneNumber;
                user.CityNameId = (City)cityComboBox.SelectedItem;

                int result = await api.UpdateParentAsync(user);

                if (result <= 0)
                {
                    MessageBox.Show("שגיאה");
                    SaveBtn.IsEnabled = true;
                    return;
                }

                MessageBox.Show("נשמר!");
            }

            SaveBtn.IsEnabled = true;
        }
    }
}