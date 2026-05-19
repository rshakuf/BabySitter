using BabySitter.Pages;
using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BabySitter.UserControls
{
    public partial class BabySitterDetailsControl : UserControl
    {
        private readonly BabySitterTeens currentTeen;
        private readonly ApiService api = new ApiService();

        public bool RequestWasSent { get; private set; } = false;

        public BabySitterDetailsControl(BabySitterTeens teen)
        {
            InitializeComponent();
            currentTeen = teen;
            DataContext = teen;

            if (LogInComputer.WhoAmI != "parent")
                SendRequestButton.Visibility = Visibility.Collapsed;

            LoadSchedules();
        }

        // ── Schedule slots ────────────────────────────────────────────────

        private async void LoadSchedules()
        {
            SchedulePanel.Children.Clear();

            var slots = await api.GetSchedulesByBabysitterIdAsync(currentTeen.Id);

            if (slots == null || !slots.Any())
            {
                NoSlotsMessage.Visibility = Visibility.Visible;
                return;
            }

            NoSlotsMessage.Visibility = Visibility.Collapsed;

            var futureSlots = slots
                .Where(s => s.DateAvailable.Date >= DateTime.Today)
                .OrderBy(s => s.DateAvailable)
                .ThenBy(s => s.Starttime)
                .ToList();

            if (!futureSlots.Any())
            {
                NoSlotsMessage.Text = "אין משמרות עתידיות רשומות";
                NoSlotsMessage.Visibility = Visibility.Visible;
                return;
            }

            foreach (var slot in futureSlots)
            {
                var btn = new Button
                {
                    Width = 145,
                    Height = 58,
                    Margin = new Thickness(5),
                    Tag = slot,
                    Content = $"{slot.DateAvailable:dd/MM/yyyy}\n{slot.Starttime:HH\\:mm} - {slot.Endtime:HH\\:mm}",
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.LightGray,
                    FontSize = 12
                };

                if (slot.IsApproved)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5D6A7"));
                    btn.ToolTip = "מאושר — לחץ לבחירה";
                }
                else if (slot.IsRequested)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082"));
                    btn.ToolTip = "יש בקשה ממתינה — לחץ לבחירה";
                }
                else
                {
                    btn.Background = Brushes.White;
                    btn.ToolTip = "לחץ לבחירה";
                }

                // All slots are clickable — color is just informational
                btn.Click += SlotButton_Click;

                var borderStyle = new Style(typeof(Border));
                borderStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(10)));
                btn.Resources[typeof(Border)] = borderStyle;

                SchedulePanel.Children.Add(btn);
            }
        }

        private void SlotButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset every slot back to its status color
            foreach (Button b in SchedulePanel.Children)
            {
                var s = b.Tag as Schedule;
                if (s == null) continue;

                if (s.IsApproved)
                    b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5D6A7"));
                else if (s.IsRequested)
                    b.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082"));
                else
                    b.Background = Brushes.White;

                b.Foreground = Brushes.Black;
            }

            var clicked = (Button)sender;
            clicked.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4"));
            clicked.Foreground = Brushes.White;

            var slot = clicked.Tag as Schedule;
            if (slot == null) return;

            // Auto-fill the form
            RequestDatePicker.SelectedDate = slot.DateAvailable.Date;
            StartTimeBox.Text = slot.Starttime.ToString("HH\\:mm");
            EndTimeBox.Text   = slot.Endtime.ToString("HH\\:mm");
            FormErrorMessage.Text = "";
        }

        // ── Send request ─────────────────────────────────────────────────

        private async void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            FormErrorMessage.Text = "";

            if (RequestDatePicker.SelectedDate == null)
            {
                FormErrorMessage.Text = "יש לבחור תאריך";
                return;
            }

            DateTime date = RequestDatePicker.SelectedDate.Value.Date;

            if (date < DateTime.Today)
            {
                FormErrorMessage.Text = "התאריך חייב להיות היום או בעתיד";
                return;
            }

            if (!TimeSpan.TryParseExact(StartTimeBox.Text.Trim(), @"hh\:mm", null, out TimeSpan start))
            {
                FormErrorMessage.Text = "שעת התחלה לא תקינה — פורמט: HH:mm";
                return;
            }

            if (!TimeSpan.TryParseExact(EndTimeBox.Text.Trim(), @"hh\:mm", null, out TimeSpan end))
            {
                FormErrorMessage.Text = "שעת סיום לא תקינה — פורמט: HH:mm";
                return;
            }

            if (end <= start)
            {
                FormErrorMessage.Text = "שעת הסיום חייבת להיות אחרי שעת ההתחלה";
                return;
            }

            var request = new Requests
            {
                ParentsId     = LogInComputer.CurrentUser as Parents,
                BabysitterId  = currentTeen,
                Status        = "pending",
                TimeOfRequest = date + start
            };

            try
            {
                SendRequestButton.IsEnabled = false;
                await api.InsertRequestAsync(request);
                RequestWasSent = true;
                Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                FormErrorMessage.Text = "שגיאה בשליחת הבקשה: " + ex.Message;
                SendRequestButton.IsEnabled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
