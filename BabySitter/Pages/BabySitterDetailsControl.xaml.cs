using BabySitter.Pages;
using ClApi;
using Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BabySitter.UserControls
{
    public partial class BabySitterDetailsControl : UserControl
    {
        private BabySitterTeens currentTeen;
        private Schedule selectedSchedule;
        private List<Schedule> schedules = new List<Schedule>();

        private ApiService api = new ApiService();

        public BabySitterDetailsControl(BabySitterTeens teen)
        {
            InitializeComponent();

            currentTeen = teen;
            this.DataContext = teen;

            LoadSchedules();
        }

        private async void LoadSchedules()
        {
            var allSchedules = await api.GetAllSchedulesAsync();

            schedules = allSchedules
                .Where(x =>
                    x.BabysitterId != null &&
                    x.BabysitterId.Id == currentTeen.Id)
                .ToList();

            SchedulePanel.Children.Clear();

            foreach (var schedule in schedules)
            {
                Button btn = new Button
                {
                    Width = 130,
                    Height = 45,
                    Margin = new Thickness(5),
                    Tag = schedule,
                    // שיניתי כאן לשמות המדויקים מתוך ה-Model שלך
                    Content = $"{schedule.DateAvailable:dd/MM/yyyy}\n{schedule.Starttime:HH:mm}-{schedule.Endtime:HH:mm}",
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };

                // בדיקת סטטוס המשמרת וצביעה בהתאם
                if (schedule.IsApproved)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5D6A7"));
                    btn.Foreground = Brushes.Black;
                    btn.IsEnabled = false;
                }
                else if (schedule.IsRequested)
                {
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082"));
                    btn.Foreground = Brushes.Black;
                    btn.IsEnabled = false;
                }
                else
                {
                    btn.Background = Brushes.White;
                    btn.Foreground = Brushes.Black;
                    btn.Click += ScheduleButton_Click;
                }

                SchedulePanel.Children.Add(btn);
            }
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Button btn in SchedulePanel.Children)
            {
                Schedule s = btn.Tag as Schedule;

                if (s != null && !s.IsRequested && !s.IsApproved)
                {
                    btn.Background = Brushes.White;
                    btn.Foreground = Brushes.Black;
                }
            }

            Button clickedButton = sender as Button;
            selectedSchedule = clickedButton.Tag as Schedule;

            clickedButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4"));
            clickedButton.Foreground = Brushes.White;

            SuccessMessage.Text = "";
        }

        private async void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSchedule == null)
            {
                MessageBox.Show("יש לבחור תאריך קודם");
                return;
            }

            selectedSchedule.ParentId = LogInComputer.CurrentUser as Parents;
            selectedSchedule.IsRequested = true;
            selectedSchedule.IsApproved = false;

            await api.UpdateScheduleAsync(selectedSchedule);

            SuccessMessage.Text = "הבקשה נשלחה בהצלחה ✔";
            selectedSchedule = null;

            LoadSchedules();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}