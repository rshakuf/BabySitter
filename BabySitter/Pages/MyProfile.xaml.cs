using BabySitter.Helpers;
using ClApi;
using Microsoft.Win32;
using Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BabySitter.Pages
{
    public partial class MyProfile : Page
    {
        private readonly ApiService api = new ApiService();
        private bool showPass = false;
        private string _pendingPhotoBase64 = null;

        private List<ChildOfParent> kids = new List<ChildOfParent>();

        public MyProfile()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var cities = await api.GetAllCitiesAsync();
                cityComboBox.ItemsSource = cities;

                if (LogInComputer.WhoAmI == "babysitter")
                {
                    var user = (BabySitterTeens)LogInComputer.CurrentUser;

                    fname.Text = user.FirstName;
                    lname.Text = user.LastName;
                    phone.Text = user.Telephone;

                    cityComboBox.SelectedItem =
                        cities.FirstOrDefault(c => c.Id == user.CityNameId?.Id);

                    pass.Password = user.Password;
                    passVisible.Text = user.Password;

                    KidsSection.Visibility = Visibility.Collapsed;
                    PhotoSection.Visibility = Visibility.Visible;

                    // Show existing profile photo or initial letter
                    ImageHelper.ApplyAvatar(user.ProfilePicture, user.FirstName,
                        AvatarLetter, AvatarImageEllipse, AvatarImageBrush);
                }
                else if (LogInComputer.WhoAmI == "parent")
                {
                    var user = (Parents)LogInComputer.CurrentUser;

                    fname.Text = user.FirstName;
                    lname.Text = user.LastName;
                    phone.Text = user.Telephone;

                    cityComboBox.SelectedItem =
                        cities.FirstOrDefault(c => c.Id == user.CityNameId?.Id);

                    pass.Password = user.Password;
                    passVisible.Text = user.Password;

                    KidsSection.Visibility = Visibility.Visible;

                    kids = await api.GetAllChildrenOfParentsAsync();

                    kids = kids
                        .Where(x => x.IdParent.Id == user.Id)
                        .ToList();

                    KidsPanel.Children.Clear();

                    int missing = user.NumOfKids - kids.Count;

                    foreach (var child in kids)
                    {
                        KidInfoControl control = new KidInfoControl(user);

                        control.FirstNameTextBoxPublic.Text = child.FirstName;
                        control.LastNameTextBoxPublic.Text = child.LastName;
                        control.BirthDatePickerPublic.SelectedDate = child.DateOfBirth;

                        control.CityComboBoxPublic.ItemsSource = cities;
                        control.CityComboBoxPublic.DisplayMemberPath = "CityName";

                        control.CityComboBoxPublic.SelectedItem =
                            cities.FirstOrDefault(c => c.Id == child.CityNameId?.Id);

                        control.Tag = child;
                        KidsPanel.Children.Add(control);
                    }

                    for (int i = 0; i < missing; i++)
                    {
                        KidInfoControl empty = new KidInfoControl(user);

                        empty.CityComboBoxPublic.ItemsSource = cities;
                        empty.CityComboBoxPublic.DisplayMemberPath = "CityName";

                        KidsPanel.Children.Add(empty);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת הנתונים: " + ex.Message);
            }
        }

        private void UploadPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title  = "בחר תמונת פרופיל",
                Filter = "קבצי תמונה|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var bytes = File.ReadAllBytes(dialog.FileName);
                _pendingPhotoBase64 = Convert.ToBase64String(bytes);

                // Preview immediately
                ImageHelper.ApplyAvatar(_pendingPhotoBase64, fname.Text,
                    AvatarLetter, AvatarImageEllipse, AvatarImageBrush);
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת התמונה: " + ex.Message);
            }
        }

        private void TogglePassword(object sender, MouseButtonEventArgs e)
        {
            showPass = !showPass;

            pass.Visibility = showPass ? Visibility.Collapsed : Visibility.Visible;
            passVisible.Visibility = showPass ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool IsValidPhone(string text)
        {
            return Regex.IsMatch(text, @"^05\d{8}$");
        }

        private async void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fname.Text) ||
                string.IsNullOrWhiteSpace(lname.Text))
            {
                MessageBox.Show("נא למלא שם פרטי ושם משפחה");
                return;
            }

            if (!IsValidPhone(phone.Text))
            {
                MessageBox.Show("טלפון לא תקין. צריך להתחיל ב־05 ולהכיל 10 ספרות");
                return;
            }

            if (cityComboBox.SelectedItem == null)
            {
                MessageBox.Show("נא לבחור עיר");
                return;
            }

            SaveBtn.IsEnabled = false;

            try
            {
                string phoneText = phone.Text.Trim();

                if (LogInComputer.WhoAmI == "babysitter")
                {
                    var user = (BabySitterTeens)LogInComputer.CurrentUser;

                    user.FirstName = fname.Text.Trim();
                    user.LastName = lname.Text.Trim();
                    user.Telephone = phoneText;
                    user.CityNameId = (City)cityComboBox.SelectedItem;
                    user.Password = showPass ? passVisible.Text : pass.Password;

                    if (_pendingPhotoBase64 != null)
                        user.ProfilePicture = _pendingPhotoBase64;

                    int result = await api.UpdateBabySitterTeenAsync(user);

                    MessageBox.Show(result > 0 ? "נשמר!" : "שגיאה בשמירה");
                }
                else if (LogInComputer.WhoAmI == "parent")
                {
                    var user = (Parents)LogInComputer.CurrentUser;

                    user.FirstName = fname.Text.Trim();
                    user.LastName = lname.Text.Trim();
                    user.Telephone = phoneText;
                    user.CityNameId = (City)cityComboBox.SelectedItem;
                    user.Password = showPass ? passVisible.Text : pass.Password;

                    int result = await api.UpdateParentAsync(user);

                    MessageBox.Show(result > 0 ? "נשמר!" : "שגיאה בשמירה");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה: " + ex.Message);
            }
            finally
            {
                SaveBtn.IsEnabled = true;
            }
        }
    }
}