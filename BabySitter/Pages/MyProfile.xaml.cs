using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClApi;
using Model;

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

                    KidsSection.Visibility = Visibility.Collapsed;
                    ScheduleSection.Visibility = Visibility.Visible;

                    await LoadScheduleSlots(user);
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
                    ScheduleSection.Visibility = Visibility.Collapsed;

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

        private bool TryReadTime(string text, out TimeSpan time)
        {
            string clean = text.Trim();

            string[] formats =
            {
                @"hh\:mm",
                @"h\:mm"
            };

            return TimeSpan.TryParseExact(
                clean,
                formats,
                CultureInfo.InvariantCulture,
                out time);
        }

        private async System.Threading.Tasks.Task LoadScheduleSlots(BabySitterTeens user)
        {
            ScheduleSlotsPanel.Children.Clear();

            List<Schedule> slots = new List<Schedule>();

            try
            {
                slots = await api.GetSchedulesByBabysitterIdAsync(user.Id);
            }
            catch
            {
                try
                {
                    var all = await api.GetAllSchedulesAsync();

                    if (all != null)
                    {
                        slots = all
                            .Where(s => s.BabysitterId != null &&
                                        s.BabysitterId.Id == user.Id)
                            .ToList();
                    }
                }
                catch
                {
                    slots = new List<Schedule>();
                }
            }

            if (slots == null || slots.Count == 0)
            {
                ScheduleSlotsPanel.Children.Add(new TextBlock
                {
                    Text = "אין זמינויות עדיין",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#79747E")),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                return;
            }

            foreach (var slot in slots.OrderBy(s => s.DateAvailable).ThenBy(s => s.Starttime))
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(14, 10, 14, 10)
                };

                Grid grid = new Grid();

                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

                string statusText;

                if (slot.IsApproved)
                    statusText = "✓ מאושר";
                else if (slot.IsRequested)
                    statusText = "⏳ ממתין";
                else
                    statusText = "פנוי";

                Color statusColor;

                if (slot.IsApproved)
                    statusColor = (Color)ColorConverter.ConvertFromString("#2E7D32");
                else if (slot.IsRequested)
                    statusColor = (Color)ColorConverter.ConvertFromString("#E65100");
                else
                    statusColor = (Color)ColorConverter.ConvertFromString("#49454F");

                TextBlock info = new TextBlock
                {
                    Text = $"{slot.DateAvailable:dd/MM/yyyy}   {slot.Starttime:HH\\:mm} - {slot.Endtime:HH\\:mm}",
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#2D3748"))
                };

                TextBlock status = new TextBlock
                {
                    Text = statusText,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(statusColor),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(12, 0, 12, 0)
                };

                Button del = new Button
                {
                    Content = "מחק",
                    FontSize = 12,
                    Height = 28,
                    Padding = new Thickness(10, 0, 10, 0),
                    Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FED7D7")),
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#C53030")),
                    BorderThickness = new Thickness(0),
                    Tag = slot,
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Style borderStyle = new Style(typeof(Border));
                borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(14)));
                del.Resources[typeof(Border)] = borderStyle;

                del.Click += DeleteSlot_Click;

                Grid.SetColumn(info, 0);
                Grid.SetColumn(status, 1);
                Grid.SetColumn(del, 2);

                grid.Children.Add(info);
                grid.Children.Add(status);
                grid.Children.Add(del);

                row.Child = grid;
                ScheduleSlotsPanel.Children.Add(row);
            }
        }

        private async void AddSlot_Click(object sender, RoutedEventArgs e)
        {
            SlotErrorMsg.Text = "";

            if (LogInComputer.WhoAmI != "babysitter")
                return;

            if (NewSlotDate.SelectedDate == null)
            {
                SlotErrorMsg.Text = "צריך לבחור תאריך";
                return;
            }

            DateTime date = NewSlotDate.SelectedDate.Value.Date;

            if (date < DateTime.Today)
            {
                SlotErrorMsg.Text = "אי אפשר להוסיף זמינות לתאריך שעבר";
                return;
            }

            if (!TryReadTime(NewSlotStart.Text, out TimeSpan start))
            {
                SlotErrorMsg.Text = "שעת התחלה לא תקינה. דוגמה: 09:00";
                return;
            }

            if (!TryReadTime(NewSlotEnd.Text, out TimeSpan end))
            {
                SlotErrorMsg.Text = "שעת סיום לא תקינה. דוגמה: 17:00";
                return;
            }

            if (end <= start)
            {
                SlotErrorMsg.Text = "שעת הסיום חייבת להיות אחרי שעת ההתחלה";
                return;
            }

            var user = (BabySitterTeens)LogInComputer.CurrentUser;

            Schedule slot = new Schedule
            {
                BabysitterId = new BabySitterTeens { Id = user.Id },
                DateAvailable = date,
                Starttime = TimeOnly.FromTimeSpan(start),
                Endtime = TimeOnly.FromTimeSpan(end),
                IsRequested = false,
                IsApproved = false
            };

            try
            {
                AddSlotBtn.IsEnabled = false;

                int result = await api.InsertScheduleAsync(slot);

                if (result <= 0)
                {
                    SlotErrorMsg.Text = "הזמינות לא נשמרה";
                    return;
                }

                NewSlotDate.SelectedDate = null;
                NewSlotStart.Text = "09:00";
                NewSlotEnd.Text = "17:00";

                await LoadScheduleSlots(user);
            }
            catch (Exception ex)
            {
                SlotErrorMsg.Text = "שגיאה בהוספה: " + ex.Message;
            }
            finally
            {
                AddSlotBtn.IsEnabled = true;
            }
        }

        private async void DeleteSlot_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn == null)
                return;

            Schedule slot = btn.Tag as Schedule;

            if (slot == null)
                return;

            MessageBoxResult confirm = MessageBox.Show(
                $"למחוק את הזמינות בתאריך {slot.DateAvailable:dd/MM/yyyy}?",
                "מחיקת זמינות",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                int result = await api.DeleteScheduleAsync(slot.Id);

                if (result <= 0)
                {
                    MessageBox.Show("הזמינות לא נמחקה");
                    return;
                }

                await LoadScheduleSlots((BabySitterTeens)LogInComputer.CurrentUser);
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה במחיקה: " + ex.Message);
            }
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