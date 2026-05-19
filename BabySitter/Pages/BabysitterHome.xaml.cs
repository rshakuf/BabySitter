using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Pages
{
    public partial class BabysitterHome : Page
    {
        private readonly ApiService api = new ApiService();

        public BabysitterHome()
        {
            InitializeComponent();
            Loaded += BabysitterHome_Loaded;
        }

        private async void BabysitterHome_Loaded(object sender, RoutedEventArgs e)
        {
            if (LogInComputer.CurrentUser is BabySitterTeens user)
            {
                WelcomeText.Text = $"שלום, {user.FirstName} <3";
                await LoadAll(user);
            }

            if (NavigationService != null)
                NavigationService.Navigated += NavigationService_Navigated;

            Unloaded += (s, _) =>
            {
                if (NavigationService != null)
                    NavigationService.Navigated -= NavigationService_Navigated;
            };
        }

        private async void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Content == this && LogInComputer.CurrentUser is BabySitterTeens user)
                await LoadAll(user);
        }

        private async System.Threading.Tasks.Task LoadAll(BabySitterTeens user)
        {
            // Show loaders immediately, before any awaits
            FreeLoadingPanel.Visibility = Visibility.Visible;
            ScheduleSlotsPanel.Visibility = Visibility.Collapsed;
            ApprovedLoadingPanel.Visibility = Visibility.Visible;
            ApprovedSlotsPanel.Visibility = Visibility.Collapsed;
            PendingLoadingPanel.Visibility = Visibility.Visible;
            PendingRequestsPanel.Visibility = Visibility.Collapsed;

            List<Schedule> slots = new List<Schedule>();
            try
            {
                var all = await api.GetAllSchedulesAsync();
                if (all != null)
                    slots = all.Where(s => s.BabysitterId?.Id == user.Id).ToList();
            }
            catch
            {
                try { slots = await api.GetSchedulesByBabysitterIdAsync(user.Id) ?? new List<Schedule>(); }
                catch { slots = new List<Schedule>(); }
            }

            // Load approved requests as fallback parent source for slots that have no ParentId
            List<Requests> approvedReqs = new List<Requests>();
            try
            {
                var allReqs = await api.GetAllRequestsAsync();
                approvedReqs = allReqs?
                    .Where(r => r.BabysitterId?.Id == user.Id && r.Status == "approved")
                    .ToList() ?? new List<Requests>();
            }
            catch { }

            var ordered = slots.OrderBy(s => s.DateAvailable).ThenBy(s => s.Starttime).ToList();

            BuildFreeSlots(ordered.Where(s => !s.IsApproved && !s.IsRequested).ToList());
            BuildApprovedSlots(ordered.Where(s => s.IsApproved).ToList(), approvedReqs);
            await LoadPendingRequests(user);
        }

        // ── Free slots ────────────────────────────────────────────────────────────

        private void BuildFreeSlots(List<Schedule> slots)
        {
            ScheduleSlotsPanel.Children.Clear();

            if (!slots.Any())
                ScheduleSlotsPanel.Children.Add(EmptyLabel("אין שעות פנויות עדיין"));
            else
                foreach (var slot in slots)
                    ScheduleSlotsPanel.Children.Add(BuildSlotRow(slot, showDelete: true));

            FreeLoadingPanel.Visibility = Visibility.Collapsed;
            ScheduleSlotsPanel.Visibility = Visibility.Visible;
        }

        // ── Approved slots ────────────────────────────────────────────────────────

        private void BuildApprovedSlots(List<Schedule> slots, List<Requests> approvedReqs)
        {
            ApprovedSlotsPanel.Children.Clear();

            if (!slots.Any())
                ApprovedSlotsPanel.Children.Add(EmptyLabel("אין שעות מאושרות עדיין"));

            foreach (var slot in slots)
            {
                // Prefer ParentId on the slot; fall back to matching approved request by date
                Parents parent = (slot.ParentId?.Id > 0) ? slot.ParentId : null;
                if (parent == null)
                {
                    var matched = approvedReqs.FirstOrDefault(r => r.TimeOfRequest.Date == slot.DateAvailable.Date);
                    parent = matched?.ParentsId ?? approvedReqs.FirstOrDefault()?.ParentsId;
                }
                ApprovedSlotsPanel.Children.Add(BuildApprovedSlotRow(slot, parent));
            }

            ApprovedLoadingPanel.Visibility = Visibility.Collapsed;
            ApprovedSlotsPanel.Visibility   = Visibility.Visible;
        }

        private Border BuildApprovedSlotRow(Schedule slot, Parents parent)
        {
            var row = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 10, 14, 10),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9")),
                BorderThickness = new Thickness(0, 0, 0, 3)
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = $"{slot.DateAvailable:dd/MM/yyyy}   {slot.Starttime:HH\\:mm} - {slot.Endtime:HH\\:mm}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"))
            });

            if (parent != null)
            {
                string name = $"{parent.FirstName} {parent.LastName}".Trim();
                if (!string.IsNullOrEmpty(name))
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"הורה: {name}",
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                        Margin = new Thickness(0, 4, 0, 0)
                    });

                if (!string.IsNullOrEmpty(parent.Telephone))
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"טלפון: {parent.Telephone}",
                        FontSize = 13,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F"))
                    });
            }

            row.Child = stack;
            return row;
        }

        private Border BuildSlotRow(Schedule slot, bool showDelete)
        {
            var row = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 10, 14, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new TextBlock
            {
                Text = $"{slot.DateAvailable:dd/MM/yyyy}   {slot.Starttime:HH\\:mm} - {slot.Endtime:HH\\:mm}",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"))
            };

            Grid.SetColumn(info, 0);
            grid.Children.Add(info);

            if (showDelete)
            {
                var del = new Button
                {
                    Content = "מחק",
                    FontSize = 12,
                    Height = 28,
                    Padding = new Thickness(10, 0, 10, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FED7D7")),
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C53030")),
                    BorderThickness = new Thickness(0),
                    Tag = slot,
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var bs = new Style(typeof(Border));
                bs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(14)));
                del.Resources[typeof(Border)] = bs;
                del.Click += DeleteSlot_Click;

                Grid.SetColumn(del, 1);
                grid.Children.Add(del);
            }

            row.Child = grid;
            return row;
        }

        // ── Pending requests ──────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadPendingRequests(BabySitterTeens user)
        {
            PendingRequestsPanel.Children.Clear();

            try
            {
                var all = await api.GetAllRequestsAsync();
                var pending = all?
                    .Where(r => r.BabysitterId?.Id == user.Id && r.Status == "pending")
                    .OrderByDescending(r => r.TimeOfRequest)
                    .ToList() ?? new List<Requests>();

                if (!pending.Any())
                {
                    PendingRequestsPanel.Children.Add(EmptyLabel("אין בקשות ממתינות"));
                }
                else
                {
                    foreach (var req in pending)
                        PendingRequestsPanel.Children.Add(BuildRequestCard(req));
                }
            }
            catch (Exception ex)
            {
                PendingRequestsPanel.Children.Add(EmptyLabel("שגיאה בטעינת בקשות: " + ex.Message));
            }
            finally
            {
                PendingLoadingPanel.Visibility = Visibility.Collapsed;
                PendingRequestsPanel.Visibility = Visibility.Visible;
            }
        }

        private Border BuildRequestCard(Requests req)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE082")),
                BorderThickness = new Thickness(0, 0, 0, 3)
            };
            card.Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 1, Opacity = 0.06, Color = Colors.Black };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            string parentName = req.ParentsId != null
                ? $"{req.ParentsId.FirstName} {req.ParentsId.LastName}"
                : "לא ידוע";

            info.Children.Add(new TextBlock
            {
                Text = $"הורה: {parentName}",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F")),
                Margin = new Thickness(0, 0, 0, 4)
            });
            info.Children.Add(new TextBlock
            {
                Text = $"תאריך: {req.TimeOfRequest:dd/MM/yyyy}  {req.TimeOfRequest:HH:mm}",
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                Margin = new Thickness(0, 0, 0, 2)
            });

            if (!string.IsNullOrEmpty(req.ParentsId?.Telephone))
                info.Children.Add(new TextBlock
                {
                    Text = $"טלפון: {req.ParentsId.Telephone}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F"))
                });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            buttons.Children.Add(MakeActionButton("אשר ✓", "#43A047", req, ApproveRequest_Click));
            buttons.Children.Add(MakeActionButton("דחה ✗", "#E53935", req, RejectRequest_Click));

            Grid.SetColumn(info, 0);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(info);
            grid.Children.Add(buttons);

            card.Child = grid;
            return card;
        }

        private Button MakeActionButton(string label, string hex, Requests req, RoutedEventHandler handler)
        {
            var btn = new Button
            {
                Content = label,
                Width = 85,
                Height = 36,
                Margin = new Thickness(6, 0, 0, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Tag = req,
                Cursor = Cursors.Hand
            };
            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(18)));
            btn.Resources[typeof(Border)] = s;
            btn.Click += handler;
            return btn;
        }

        private async void ApproveRequest_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req == null) return;

            req.Status = "approved";
            await api.UpdateRequestAsync(req);

            // Update the matching schedule slot so ParentId and IsApproved are persisted
            try
            {
                var allSlots = await api.GetAllSchedulesAsync();
                var slot = allSlots?.FirstOrDefault(s =>
                    s.BabysitterId?.Id == req.BabysitterId?.Id &&
                    s.IsRequested && !s.IsApproved);
                if (slot != null)
                {
                    slot.IsApproved = true;
                    slot.ParentId = req.ParentsId;
                    await api.UpdateScheduleAsync(slot);
                }
            }
            catch { }

            if (LogInComputer.CurrentUser is BabySitterTeens user)
                await LoadAll(user);
        }

        private async void RejectRequest_Click(object sender, RoutedEventArgs e)
        {
            var req = (sender as Button)?.Tag as Requests;
            if (req == null) return;
            req.Status = "rejected";
            await api.UpdateRequestAsync(req);
            if (LogInComputer.CurrentUser is BabySitterTeens user)
                await LoadAll(user);
        }

        // ── Slot add / delete ─────────────────────────────────────────────────────

        private async void AddSlot_Click(object sender, RoutedEventArgs e)
        {
            SlotErrorMsg.Text = "";

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

            var slot = new Schedule
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

                await LoadAll(user);
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
            var btn = sender as Button;
            if (btn == null) return;

            var slot = btn.Tag as Schedule;
            if (slot == null) return;

            var confirm = MessageBox.Show(
                $"למחוק את הזמינות בתאריך {slot.DateAvailable:dd/MM/yyyy}?",
                "מחיקת זמינות",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                int result = await api.DeleteScheduleAsync(slot.Id);
                if (result <= 0) { MessageBox.Show("הזמינות לא נמחקה"); return; }
                if (LogInComputer.CurrentUser is BabySitterTeens user)
                    await LoadAll(user);
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה במחיקה: " + ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private bool TryReadTime(string text, out TimeSpan time)
        {
            string[] formats = { @"hh\:mm", @"h\:mm" };
            return TimeSpan.TryParseExact(text.Trim(), formats, CultureInfo.InvariantCulture, out time);
        }

        private TextBlock EmptyLabel(string text) => new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E")),
            Margin = new Thickness(0, 0, 0, 8)
        };

        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MyProfile());
        }
    }
}
