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
        private bool   showPass = false;
        private string _newProfilePicBase64   = null;   // preview only – raw bytes as Base64
        private string _newProfilePicFileName = null;   // filename returned by the server (stored in DB)

        private List<ChildOfParent> kids   = new List<ChildOfParent>();
        private List<City>          _cities = new List<City>();

        public MyProfile()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                _cities = await api.GetAllCitiesAsync();
                var cities = _cities;
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

                    // Show avatar section and load current picture
                    AvatarSection.Visibility = Visibility.Visible;
                    ApplyAvatarToProfile(user.ProfilePicture, user.FirstName);
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
                }
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה בטעינת הנתונים: " + ex.Message, Window.GetWindow(this));
            }
        }

        private static bool IsValidEmail(string email) =>
            Regex.IsMatch(email, @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$");

        private void Mail_LostFocus(object sender, RoutedEventArgs e)
        {
            string value = mail.Text.Trim();
            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
            {
                mail.Background         = System.Windows.Media.Brushes.LightCoral;
                mail.ToolTip            = "כתובת אימייל לא תקינה";
                MailErrorMsg.Text       = "כתובת מייל לא תקינה";
                MailErrorMsg.Visibility = Visibility.Visible;
            }
            else
            {
                mail.ClearValue(TextBox.BackgroundProperty);
                mail.ToolTip            = null;
                MailErrorMsg.Visibility = Visibility.Collapsed;
            }
        }

        private void Mail_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Clear error highlight as soon as the user starts correcting
            mail.ClearValue(TextBox.BackgroundProperty);
            mail.ToolTip            = null;
            MailErrorMsg.Visibility = Visibility.Collapsed;
        }

        // ── Profile picture ───────────────────────────────────────────────────────

        /// Opens a file dialog, previews the image immediately, then uploads it to the
        /// server which saves the file and returns a filename to store in the DB.
        private async void PickProfilePic_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "בחר תמונת פרופיל",
                Filter = "קבצי תמונה|*.jpg;*.jpeg;*.png;*.bmp;*.gif|כל הקבצים|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                byte[] bytes = File.ReadAllBytes(dlg.FileName);

                // Validate reasonable size (max 5 MB)
                if (bytes.Length > 5 * 1024 * 1024)
                {
                    CustomDialogHelper.ShowWarning("התמונה גדולה מדי (מקסימום 5MB). בחר תמונה קטנה יותר.", Window.GetWindow(this));
                    return;
                }

                // Show preview immediately (Base64 in memory, not yet saved)
                _newProfilePicBase64 = Convert.ToBase64String(bytes);
                ApplyAvatarToProfile(_newProfilePicBase64, fname.Text.Trim());

                // Upload to server → get back the saved filename
                var user = (BabySitterTeens)LogInComputer.CurrentUser;
                _newProfilePicFileName = await api.UploadProfilePictureAsync(user.Id, _newProfilePicBase64);
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה בטעינת התמונה: " + ex.Message, Window.GetWindow(this));
            }
        }

        /// Displays base64 image (or initial letter) in the profile-page avatar circle.
        private void ApplyAvatarToProfile(string base64, string firstName)
        {
            var bmp = ImageHelper.BitmapFromBase64(base64);
            if (bmp != null)
            {
                AvatarImageBrushProfile.ImageSource       = bmp;
                AvatarImageEllipseProfile.Visibility      = Visibility.Visible;
                AvatarLetterProfile.Visibility            = Visibility.Collapsed;
                DeletePicLink.Visibility                  = Visibility.Visible;
            }
            else
            {
                AvatarImageEllipseProfile.Visibility = Visibility.Collapsed;
                AvatarLetterProfile.Visibility       = Visibility.Visible;
                AvatarLetterProfile.Text             = firstName?.Length > 0 ? firstName[0].ToString().ToUpper() : "?";
                DeletePicLink.Visibility             = Visibility.Collapsed;
            }
        }

        private async void DeletePic_Click(object sender, MouseButtonEventArgs e)
        {
            if (!CustomDialogHelper.ShowConfirm("האם למחוק את תמונת הפרופיל?", "מחיקת תמונה", Window.GetWindow(this)))
                return;

            var user = (BabySitterTeens)LogInComputer.CurrentUser;
            bool ok = await api.DeleteProfilePictureAsync(user.Id);
            if (!ok)
            {
                CustomDialogHelper.ShowError("שגיאה במחיקת התמונה. נסה שוב.", Window.GetWindow(this));
                return;
            }
            user.ProfilePicture    = null;
            _newProfilePicBase64   = null;
            _newProfilePicFileName = null;
            ApplyAvatarToProfile(null, user.FirstName);
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void AddKid_Click(object sender, RoutedEventArgs e)
        {
            var user = (Parents)LogInComputer.CurrentUser;
            var card = new KidInfoControl(user, _cities)
            {
                Margin = new System.Windows.Thickness(8)
            };
            KidsPanel.Children.Add(card);
        }

        private async void SaveChanges(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(fname.Text) ||
                string.IsNullOrWhiteSpace(lname.Text))
            {
                CustomDialogHelper.ShowWarning("נא למלא שם פרטי ושם משפחה", Window.GetWindow(this));
                return;
            }

            if (!IsValidPhone(phone.Text))
            {
                CustomDialogHelper.ShowWarning("טלפון לא תקין. צריך להתחיל ב־05 ולהכיל 10 ספרות", Window.GetWindow(this));
                return;
            }

            if (cityComboBox.SelectedItem == null)
            {
                CustomDialogHelper.ShowWarning("נא לבחור עיר", Window.GetWindow(this));
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
                        mail.Background         = System.Windows.Media.Brushes.LightCoral;
                        mail.ToolTip            = "כתובת אימייל לא תקינה";
                        MailErrorMsg.Text       = "כתובת מייל לא תקינה";
                        MailErrorMsg.Visibility = Visibility.Visible;
                        mail.Focus();
                        return;
                    }
                    mail.ClearValue(TextBox.BackgroundProperty);
                    mail.ToolTip            = null;
                    MailErrorMsg.Visibility = Visibility.Collapsed;

                    if (!int.TryParse(pricePerHour.Text.Trim(), out int price) || price <= 0)
                    {
                        PriceErrorMsg.Text       = "נא להזין מחיר תקין (מספר חיובי)";
                        PriceErrorMsg.Visibility = Visibility.Visible;
                        pricePerHour.Focus();
                        return;
                    }
                    PriceErrorMsg.Visibility = Visibility.Collapsed;

                    if (price <= PriceWarningHelper.LowPriceThreshold &&
                        !PriceWarningHelper.ConfirmLowPrice(price, Window.GetWindow(this)))
                        return;

                    if (price >= PriceWarningHelper.HighPriceThreshold &&
                        !PriceWarningHelper.ConfirmHighPrice(price, Window.GetWindow(this)))
                        return;

                    user.FirstName      = fname.Text.Trim();
                    user.LastName       = lname.Text.Trim();
                    user.Telephone      = phoneText;
                    user.CityNameId     = (City)cityComboBox.SelectedItem;
                    user.Password       = showPass ? passVisible.Text : pass.Password;
                    user.Mail           = mailText;
                    user.PriceForAnHour = price;

                    // Send only the short filename to the server; send null if no new photo
                    // (so the server skips overwriting the existing profilePicture in the DB).
                    string existingPicBase64 = user.ProfilePicture;
                    string base64ToRestore   = null;
                    if (_newProfilePicFileName != null)
                    {
                        base64ToRestore     = _newProfilePicBase64;
                        user.ProfilePicture = _newProfilePicFileName;
                    }
                    else
                    {
                        user.ProfilePicture = null; // server will skip profilePicture column
                    }

                    int result = await api.UpdateBabySitterTeenAsync(user);

                    // Restore the in-memory Base64 regardless of success/failure
                    user.ProfilePicture = base64ToRestore ?? existingPicBase64;

                    if (result > 0)
                    {
                        _newProfilePicBase64   = null;
                        _newProfilePicFileName = null;
                        CustomDialogHelper.ShowSuccess("נשמר!", Window.GetWindow(this));
                    }
                    else
                    {
                        CustomDialogHelper.ShowError("שגיאה בשמירה", Window.GetWindow(this));
                    }
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

                    if (result > 0)
                    {
                        // Save / update all kid cards
                        bool allKidsOk = true;
                        foreach (UIElement el in KidsPanel.Children)
                        {
                            if (el is KidInfoControl kidCard)
                            {
                                bool ok = await kidCard.SaveAsync();
                                if (!ok) allKidsOk = false;
                            }
                        }
                        CustomDialogHelper.ShowSuccess(allKidsOk ? "נשמר!" : "הפרטים האישיים נשמרו, אך חלק מפרטי הילדים לא הושלמו", Window.GetWindow(this));
                    }
                    else
                    {
                        CustomDialogHelper.ShowError("שגיאה בשמירה", Window.GetWindow(this));
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה: " + ex.Message, Window.GetWindow(this));
            }
            finally
            {
                SaveBtn.IsEnabled = true;
            }
        }
    }
}
