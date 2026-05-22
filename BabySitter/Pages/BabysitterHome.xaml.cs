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
        private List<BabySitterRate> _myRates = new();

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

            // Fetch schedules, requests and rates in parallel
            var schedulesTask = api.GetAllSchedulesAsync();
            var requestsTask  = api.GetAllRequestsAsync();
            var ratesTask     = api.GetAllBabySitterRatesAsync();
            await System.Threading.Tasks.Task.WhenAll(schedulesTask, requestsTask, ratesTask);

            _myRates = (ratesTask.Result ?? new System.Collections.Generic.List<BabySitterRate>())
                .Where(r => r.IdBabySitter?.Id == user.Id)
                .ToList();
            UpdateMyStars();

            var slots = (schedulesTask.Result ?? Enumerable.Empty<Schedule>())
                .Where(s => s.BabysitterId?.Id == user.Id && s.DateAvailable.Date >= DateTime.Today)
                .OrderBy(s => s.DateAvailable).ThenBy(s => s.Starttime)
                .ToList();

            var allReqs = requestsTask.Result ?? new List<Requests>();

            var approvedReqs = allReqs
                .Where(r => r.BabysitterId?.Id == user.Id && r.Status == "approved"
                         && r.TimeOfRequest.Date >= DateTime.Today)
                .OrderBy(r => r.TimeOfRequest)
                .ToList();

            var pendingReqs = allReqs
                .Where(r => r.BabysitterId?.Id == user.Id && r.Status == "pending"
                         && r.TimeOfRequest.Date >= DateTime.Today)
                .OrderByDescending(r => r.TimeOfRequest)
                .ToList();

            var approvedDates = approvedReqs.Select(r => r.TimeOfRequest.Date).ToHashSet();
            BuildFreeSlots(slots.Where(s => !approvedDates.Contains(s.DateAvailable.Date)).ToList());
            BuildApprovedSlots(approvedReqs, slots);
            BuildPendingRequests(pendingReqs);
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

        private void BuildApprovedSlots(List<Requests> approvedReqs, List<Schedule> allSlots)
        {
            ApprovedSlotsPanel.Children.Clear();

            if (!approvedReqs.Any())
                ApprovedSlotsPanel.Children.Add(EmptyLabel("אין שעות מאושרות עדיין"));

            foreach (var req in approvedReqs)
            {
                // Find matching schedule to get end time
                var matchingSlot = allSlots.FirstOrDefault(s =>
                    s.DateAvailable.Date == req.TimeOfRequest.Date &&
                    s.Starttime == TimeOnly.FromTimeSpan(req.TimeOfRequest.TimeOfDay));

                ApprovedSlotsPanel.Children.Add(BuildApprovedSlotRow(req, matchingSlot));
            }

            ApprovedLoadingPanel.Visibility = Visibility.Collapsed;
            ApprovedSlotsPanel.Visibility   = Visibility.Visible;
        }

        private Border BuildApprovedSlotRow(Requests req, Schedule matchingSlot)
        {
            var row = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 12, 16, 12),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C8E6C9")),
                BorderThickness = new Thickness(0, 0, 0, 3)
            };
            row.Effect = new DropShadowEffect { BlurRadius = 8, ShadowDepth = 1, Opacity = 0.05, Color = Colors.Black };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Info stack (right side in RTL)
            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            string endTime = matchingSlot != null
                ? matchingSlot.Endtime.ToString("HH\\:mm")
                : req.LenghtTime > 0
                    ? (req.TimeOfRequest + TimeSpan.FromHours(req.LenghtTime)).ToString("HH:mm")
                    : null;

            string timeText = endTime != null
                ? $"{req.TimeOfRequest:dd/MM/yyyy}   {req.TimeOfRequest:HH:mm} - {endTime}"
                : $"{req.TimeOfRequest:dd/MM/yyyy}   {req.TimeOfRequest:HH:mm}";

            stack.Children.Add(new TextBlock
            {
                Text = timeText,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"))
            });

            var parent = req.ParentsId;
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

            // Cancel button (left side in RTL)
            var cancelBtn = new Button
            {
                Content = "ביטול שמרת",
                Height = 36,
                Padding = new Thickness(14, 0, 14, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD")),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                Tag = new Tuple<Requests, Schedule>(req, matchingSlot)
            };
            var bs = new Style(typeof(Border));
            bs.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(18)));
            cancelBtn.Resources[typeof(Border)] = bs;
            cancelBtn.Click += CancelApprovedSlot_Click;

            Grid.SetColumn(stack, 0);
            Grid.SetColumn(cancelBtn, 1);
            grid.Children.Add(stack);
            grid.Children.Add(cancelBtn);

            row.Child = grid;
            return row;
        }

        private async void CancelApprovedSlot_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is not Tuple<Requests, Schedule> tag) return;

            var req = tag.Item1;
            var slot = tag.Item2;

            string dateText = req.TimeOfRequest.ToString("dd/MM/yyyy");
            string endForDialog = slot != null
                ? slot.Endtime.ToString("HH\\:mm")
                : req.LenghtTime > 0
                    ? (req.TimeOfRequest + TimeSpan.FromHours(req.LenghtTime)).ToString("HH:mm")
                    : null;
            string timeText = endForDialog != null
                ? $"{req.TimeOfRequest:HH:mm} - {endForDialog}  ({req.LenghtTime} שעות)"
                : req.TimeOfRequest.ToString("HH:mm");

            bool confirmed = ShowCancelConfirmationDialog(dateText, timeText, req.ParentsId);
            if (!confirmed) return;

            req.Status = "cancelled_by_babysitter";
            await api.UpdateRequestAsync(req);

            if (LogInComputer.CurrentUser is BabySitterTeens user)
                await LoadAll(user);
        }

        private bool ShowCancelConfirmationDialog(string dateText, string timeText, Parents parent)
        {
            bool confirmed = false;

            var dlg = new Window
            {
                Width = 440,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                FlowDirection = FlowDirection.RightToLeft
            };

            var outer = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Margin = new Thickness(10),
                Padding = new Thickness(0)
            };
            outer.Effect = new DropShadowEffect { BlurRadius = 30, ShadowDepth = 6, Opacity = 0.18, Color = Colors.Black };

            var mainStack = new StackPanel();

            // Header strip
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding = new Thickness(24, 16, 24, 16)
            };
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            headerStack.Children.Add(new TextBlock
            {
                Text = "⚠",
                FontSize = 22,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "ביטול שמרת",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Child = headerStack;
            mainStack.Children.Add(header);

            // Body
            var body = new StackPanel { Margin = new Thickness(28, 20, 28, 20) };

            body.Children.Add(new TextBlock
            {
                Text = "האם אתה בטוח שברצונך לבטל את השמרת?",
                FontSize = 15,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#212121")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            });

            // Details card
            var detailCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8F8")),
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var detailStack = new StackPanel();
            detailStack.Children.Add(new TextBlock
            {
                Text = $"📅  תאריך: {dateText}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                Margin = new Thickness(0, 0, 0, 6)
            });
            detailStack.Children.Add(new TextBlock
            {
                Text = $"🕐  שעות: {timeText}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                Margin = new Thickness(0, 0, 0, 6)
            });
            if (parent != null)
                detailStack.Children.Add(new TextBlock
                {
                    Text = $"👤  הורה: {parent.FirstName} {parent.LastName}",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"))
                });
            detailCard.Child = detailStack;
            body.Children.Add(detailCard);

            // Buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var yesBtn = new Button
            {
                Content = "כן, בטל שמרת",
                Width = 150, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")),
                Foreground = Brushes.White,
                FontSize = 14, FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            var ys = new Style(typeof(Border)); ys.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            yesBtn.Resources[typeof(Border)] = ys;
            yesBtn.Click += (s, e) => { confirmed = true; dlg.Close(); };

            var noBtn = new Button
            {
                Content = "לא, חזור",
                Width = 120, Height = 42,
                Margin = new Thickness(8, 0, 8, 0),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242")),
                FontSize = 14, FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            var ns = new Style(typeof(Border)); ns.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(21)));
            noBtn.Resources[typeof(Border)] = ns;
            noBtn.Click += (s, e) => dlg.Close();

            btnRow.Children.Add(yesBtn);
            btnRow.Children.Add(noBtn);
            body.Children.Add(btnRow);

            mainStack.Children.Add(body);
            outer.Child = mainStack;
            dlg.Content = outer;
            dlg.ShowDialog();
            return confirmed;
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

        private void BuildPendingRequests(List<Requests> pending)
        {
            PendingRequestsPanel.Children.Clear();

            if (!pending.Any())
                PendingRequestsPanel.Children.Add(EmptyLabel("אין בקשות ממתינות"));
            else
                foreach (var req in pending)
                    PendingRequestsPanel.Children.Add(BuildRequestCard(req));

            PendingLoadingPanel.Visibility = Visibility.Collapsed;
            PendingRequestsPanel.Visibility = Visibility.Visible;
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
            string pendingEndTime = req.LenghtTime > 0
                ? (req.TimeOfRequest + TimeSpan.FromHours(req.LenghtTime)).ToString("HH:mm")
                : null;
            string pendingTimeText = pendingEndTime != null
                ? $"{req.TimeOfRequest:dd/MM/yyyy}   {req.TimeOfRequest:HH:mm} - {pendingEndTime}  ({req.LenghtTime} שעות)"
                : $"{req.TimeOfRequest:dd/MM/yyyy}   {req.TimeOfRequest:HH:mm}";

            info.Children.Add(new TextBlock
            {
                Text = $"תאריך: {pendingTimeText}",
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
                Endtime = TimeOnly.FromTimeSpan(end)
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

        // ── Rating display ────────────────────────────────────────────────────────

        private void UpdateMyStars()
        {
            if (!_myRates.Any())
            {
                StarsRow.Visibility = Visibility.Collapsed;
                return;
            }

            double avg = _myRates.Average(r => r.Stars);
            int count  = _myRates.Count;

            var gold = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            var gray = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            var stars = new[] { BStar1, BStar2, BStar3, BStar4, BStar5 };
            int filled = (int)Math.Round(avg);
            for (int i = 0; i < 5; i++)
                stars[i].Foreground = i < filled ? gold : gray;

            BRatingText.Text = $"{avg:F1}  ({count} דירוגים)";
            StarsRow.Visibility = Visibility.Visible;
        }

        private void RatingDetails_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_myRates.Any()) return;

            var dlg = new Window
            {
                Title = "פרטי דירוגים",
                Width = 460,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F2FA")),
                FlowDirection = FlowDirection.RightToLeft
            };

            var outer = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(20),
                Margin = new Thickness(16),
                Padding = new Thickness(0)
            };
            outer.Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 3, Opacity = 0.1, Color = Colors.Black };

            var stack = new StackPanel();

            // Header
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6750A4")),
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                Padding = new Thickness(24, 16, 24, 16)
            };
            double avg = _myRates.Average(r => r.Stars);
            header.Child = new TextBlock
            {
                Text = $"הדירוגים שלי  •  ממוצע {avg:F1} ★",
                FontSize = 18, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            stack.Children.Add(header);

            // List of reviews
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(16, 12, 16, 16) };
            var reviewsPanel = new StackPanel();

            var gold = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            var gray = new SolidColorBrush(Color.FromRgb(204, 204, 204));

            foreach (var rate in _myRates.OrderByDescending(r => r.DateOfRate))
            {
                var card = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9F6FF")),
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(16, 12, 16, 12),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                    BorderThickness = new Thickness(1)
                };

                var cardStack = new StackPanel();

                // Parent name + date
                string parentName = rate.IdParent != null
                    ? $"{rate.IdParent.FirstName} {rate.IdParent.LastName}".Trim()
                    : "הורה לא ידוע";
                string dateStr = rate.DateOfRate != default ? rate.DateOfRate.ToString("dd/MM/yyyy") : "";

                var topRow = new Grid();
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var parentTb = new TextBlock
                {
                    Text = parentName,
                    FontSize = 14, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1C1B1F"))
                };
                var dateTb = new TextBlock
                {
                    Text = dateStr,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E")),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(parentTb, 0);
                Grid.SetColumn(dateTb, 1);
                topRow.Children.Add(parentTb);
                topRow.Children.Add(dateTb);
                cardStack.Children.Add(topRow);

                // Stars
                var starsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                for (int i = 1; i <= 5; i++)
                    starsPanel.Children.Add(new TextBlock
                    {
                        Text = "★",
                        FontSize = 20,
                        Margin = new Thickness(0, 0, 2, 0),
                        Foreground = i <= rate.Stars ? gold : gray
                    });
                starsPanel.Children.Add(new TextBlock
                {
                    Text = $"  {rate.Stars}/5",
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#49454F")),
                    VerticalAlignment = VerticalAlignment.Center
                });
                cardStack.Children.Add(starsPanel);

                card.Child = cardStack;
                reviewsPanel.Children.Add(card);
            }

            scroll.Content = reviewsPanel;
            stack.Children.Add(scroll);

            outer.Child = stack;
            dlg.Content = outer;
            dlg.ShowDialog();
        }

        private void MyProfile_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MyProfile());
        }

        private void GoToHistory_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new JobHistoryPage());
        }
    }
}
