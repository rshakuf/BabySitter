using BabySitter.Helpers;
using ClApi;
using Model;
using System;
using System.Collections.Generic;
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

                    KidsSection.Visibility       = Visibility.Collapsed;
                    EmailPriceSection.Visibility = Visibility.Visible;
                    mail.Text         = user.Mail ?? "";
                    pricePerHour.Text = user.PriceForAnHour > 0 ? user.PriceForAnHour.ToString() : "";
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

                    var allKids = await api.GetAllChildrenOfParentsAsync();
                    kids = allKids
                        .Where(x => x.IdParent?.Id == user.Id)
                        .ToList();

                    KidsPanel.Children.Clear();

                    foreach (var child in kids)
                        KidsPanel.Children.Add(new KidInfoControl(user, cities, child));

                    int missing = user.NumOfKids - kids.Count;
                    for (int i = 0; i < missing; i++)
                        KidsPanel.Children.Add(new KidInfoControl(user, cities));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת הנתונים: " + ex.Message);
            }
        }

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$");

        private void Mail_LostFocus(object sender, RoutedEventArgs e)
        {
            string value = mail.Text.Trim();
            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
            {
                MailErrorMsg.Text       = "כתובת מייל לא תקינה";
                MailErrorMsg.Visibility = Visibility.Visible;
            }
            else
            {
                MailErrorMsg.Visibility = Visibility.Collapsed;
            }
        }

        private void TogglePassword(object sender, MouseButtonEventArgs e)
        {
            showPass = !showPass;

            pass.Visibility        = showPass ? Visibility.Collapsed : Visibility.Visible;
            passVisible.Visibility = showPass ? Visibility.Visible   : Visibility.Collapsed;
        }

        private void Phone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void Price_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool IsValidPhone(string text) =>
            Regex.IsMatch(text, @"^05\d{8}$");

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

                    string mailText = mail.Text.Trim();
                    if (!string.IsNullOrEmpty(mailText) && !IsValidEmail(mailText))
                    {
                        MailErrorMsg.Text       = "כתובת מייל לא תקינה";
                        MailErrorMsg.Visibility = Visibility.Visible;
                        mail.Focus();
                        return;
                    }

                    if (!int.TryParse(pricePerHour.Text.Trim(), out int price) || price <= 0)
                    {
                        PriceErrorMsg.Text       = "נא להזין מחיר תקין (מספר חיובי)";
                        PriceErrorMsg.Visibility = Visibility.Visible;
                        pricePerHour.Focus();
                        return;
                    }
                    PriceErrorMsg.Visibility = Visibility.Collapsed;

                    if (price >= 80 &&
                        !PriceWarningHelper.ConfirmHighPrice(price, Window.GetWindow(this)))
                        return;

                    user.FirstName      = fname.Text.Trim();
                    user.LastName       = lname.Text.Trim();
                    user.Telephone      = phoneText;
                    user.CityNameId     = (City)cityComboBox.SelectedItem;
                    user.Password       = showPass ? passVisible.Text : pass.Password;
                    user.Mail           = mailText;
                    user.PriceForAnHour = price;

                    int result = await api.UpdateBabySitterTeenAsync(user);
                    MessageBox.Show(result > 0 ? "נשמר!" : "שגיאה בשמירה");
                }
                else if (LogInComputer.WhoAmI == "parent")
                {
                    var user = (Parents)LogInComputer.CurrentUser;

                    user.FirstName  = fname.Text.Trim();
                    user.LastName   = lname.Text.Trim();
                    user.Telephone  = phoneText;
                    user.CityNameId = (City)cityComboBox.SelectedItem;
                    user.Password   = showPass ? passVisible.Text : pass.Password;

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
