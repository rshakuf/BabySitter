using BabySitter.Pages;
using ClApi;
using Model;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModel;

namespace BabySitter.UserControls
{
    public partial class BabySitterDetailsControl : UserControl
    {
        private BabySitterTeens currentTeen;
        private Schedule selectedSchedule;
        private List<Schedule> schedules = new List<Schedule>();

        public BabySitterDetailsControl(BabySitterTeens teen)
        {
            InitializeComponent();

            currentTeen = teen;
            this.DataContext = teen;

            LoadSchedules();
        }

        private void LoadSchedules()
        {
            ScheduleDB db = new ScheduleDB();

            schedules = db.SelectAll()
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
                    Content = $"{schedule.DayOfWeek}\n{schedule.StartTime:HH:mm}-{schedule.EndTime:HH:mm}",
                    Background = Brushes.White,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1)
                };

                if (schedule.IsRequested)
                {
                    btn.Background = Brushes.LightGray;
                    btn.IsEnabled = false;
                }

                btn.Click += ScheduleButton_Click;

                SchedulePanel.Children.Add(btn);
            }
        }

        private void ScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (Button btn in SchedulePanel.Children)
            {
                Schedule s = btn.Tag as Schedule;

                if (s != null && !s.IsRequested)
                {
                    btn.Background = Brushes.White;
                    btn.Foreground = Brushes.Black;
                }
            }

            Button clickedButton = sender as Button;
            selectedSchedule = clickedButton.Tag as Schedule;

            clickedButton.Background = Brushes.Red;
            clickedButton.Foreground = Brushes.White;

            SuccessMessage.Text = "";
        }

        private void SendRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSchedule == null)
            {
                MessageBox.Show("יש לבחור תאריך קודם");
                return;
            }

            selectedSchedule.ParentId =
                LogInComputer.CurrentUser as Parents;

            selectedSchedule.IsRequested = true;
            selectedSchedule.IsApproved = false;

            ScheduleDB db = new ScheduleDB();
            db.Update(selectedSchedule);
            db.Save();

            SuccessMessage.Text = "הבקשה נשלחה בהצלחה ✔";

            LoadSchedules();
            selectedSchedule = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}