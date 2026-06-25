using BabySitter.Helpers;
using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BabySitter.Pages
{
    public partial class MyScheduleViewPage : Page
    {
        // ── States ─────────────────────────────────────────────────────────────────
        private enum SlotState { Available, Taken, PendingApproval, MarkedForRemoval, ToAdd, AlreadyScheduled, Past }

        // ── Fields ─────────────────────────────────────────────────────────────────
        private readonly BabySitterTeens   _teen;
        private readonly ApiService        _api = new ApiService();

        private List<DateTime>             _allDates     = new();
        private int                        _currentIndex = 0;
        private Dictionary<int, SlotState> _hourStates   = new();

        // Pending edits for the current day
        private readonly HashSet<int> _markedForRemoval = new();
        private readonly HashSet<int> _hoursToAdd       = new();

        // Raw data stashed after load
        private List<Schedule> _mySlots      = new();
        private List<Requests> _approvedReqs = new();   // fully booked — non-clickable red
        private List<Requests> _pendingReqs  = new();   // waiting for babysitter's approval — amber

        // Hour range: 7 → 24 (24 is displayed as "00:00", represents 23:00–00:00 slot)
        private const int DayStart = 7;
        private const int DayEnd   = 25;   // exclusive → shows 7..24 (00:00)

        // ── Constructor ────────────────────────────────────────────────────────────

        public MyScheduleViewPage(BabySitterTeens teen)
        {
            InitializeComponent();
            _teen = teen;
            BindHeader();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        // ── Header ─────────────────────────────────────────────────────────────────

        private void BindHeader()
        {
            BabysitterNameLabel.Text  = $"{_teen.FirstName} {_teen.LastName}";
            BabysitterCityLabel.Text  = _teen.CityNameId?.CityName ?? "";
            BabysitterPriceLabel.Text = $"₪{_teen.PriceForAnHour} לשעה";
            ImageHelper.ApplyAvatar(_teen.ProfilePicture, _teen.FirstName,
                AvatarLetter, AvatarImageEllipse, AvatarImageBrush);
        }

        // ── Data loading ───────────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var schedulesTask = _api.GetAllSchedulesAsync();
                var requestsTask  = _api.GetAllRequestsAsync();
                await System.Threading.Tasks.Task.WhenAll(schedulesTask, requestsTask);

                _mySlots = (schedulesTask.Result ?? Enumerable.Empty<Schedule>())
                    .Where(s => s.BabysitterId?.Id == _teen.Id)
                    .ToList();

                var myReqs = (requestsTask.Result ?? Enumerable.Empty<Requests>())
                    .Where(r => r.BabysitterId?.Id == _teen.Id)
                    .ToList();

                _approvedReqs = myReqs.Where(r => r.Status == "approved").ToList();
                _pendingReqs  = myReqs.Where(r => r.Status == "pending").ToList();

                _allDates = _mySlots
                    .Select(s => s.DateAvailable.Date)
                    .Where(d => d >= DateTime.Today)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                if (!_allDates.Any()) { ShowEmpty(); return; }

                _currentIndex = Math.Min(_currentIndex, _allDates.Count - 1);
                RenderDay();
            }
            catch (Exception ex)
            {
                CustomDialogHelper.ShowError("שגיאה בטעינת לוח הזמנים: " + ex.Message, Window.GetWindow(this));
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // ── Hour-expansion helpers ─────────────────────────────────────────────────

        private HashSet<int> ExpandSlotHours(DateTime date) =>
            ExpandSlotHoursStatic(date, _mySlots);

        private static HashSet<int> ExpandSlotHoursStatic(DateTime date, IEnumerable<Schedule> slots)
        {
            var h = new HashSet<int>();
            foreach (var s in slots.Where(s => s.DateAvailable.Date == date))
            {
                // Midnight end time (00:00) is stored as TimeOnly(0,0) — treat as hour 24
                int endHour = (s.Endtime.Hour == 0 && s.Starttime.Hour > 0) ? 24 : s.Endtime.Hour;
                for (int i = s.Starttime.Hour; i < endHour; i++)
                    h.Add(i);
            }
            return h;
        }

        private HashSet<int> ExpandRequestHours(DateTime date, IEnumerable<Requests> reqs)
        {
            var h = new HashSet<int>();
            foreach (var r in reqs.Where(r => r.TimeOfRequest.Date == date))
            {
                int len = r.LenghtTime > 0 ? r.LenghtTime : 1;
                for (int i = r.TimeOfRequest.Hour; i < r.TimeOfRequest.Hour + len; i++)
                    h.Add(i);
            }
            return h;
        }

        // ── Rendering ─────────────────────────────────────────────────────────────

        private void RenderDay()
        {
            _markedForRemoval.Clear();
            _hoursToAdd.Clear();
            HourSlotsPanel.Children.Clear();
            _hourStates.Clear();

            var  date     = _allDates[_currentIndex];
            CurrentDateLabel.Text = date.ToString("dd/MM/yy");

            var  scheduled = ExpandSlotHours(date);
            var  approved  = ExpandRequestHours(date, _approvedReqs);
            var  pending   = ExpandRequestHours(date, _pendingReqs);
            bool isPast    = date.Date < DateTime.Today;
            bool isToday   = date.Date == DateTime.Today;
            int  nowHour   = DateTime.Now.Hour;

            // Show every hour that is scheduled, approved, or pending
            var allHours = new HashSet<int>(scheduled);
            allHours.UnionWith(approved);
            allHours.UnionWith(pending);

            foreach (int h in allHours.OrderBy(x => x))
            {
                SlotState state;
                if      (isPast || (isToday && h <= nowHour)) state = SlotState.Past;
                else if (approved.Contains(h))                state = SlotState.Taken;
                else if (pending.Contains(h))                 state = SlotState.PendingApproval;
                else                                          state = SlotState.Available;

                _hourStates[h] = state;
                HourSlotsPanel.Children.Add(BuildHourCell(h, state));
            }

            // "Add hours" section
            if (!isPast)
                BuildAddHoursSection(date, scheduled, approved, pending, isToday, nowHour);

            PrevPanel.Visibility = _currentIndex > 0                   ? Visibility.Visible : Visibility.Hidden;
            NextPanel.Visibility = _currentIndex < _allDates.Count - 1 ? Visibility.Visible : Visibility.Hidden;
            PrevDateLabel.Text   = _currentIndex > 0                   ? _allDates[_currentIndex - 1].ToString("dd/MM/yy") : "";
            NextDateLabel.Text   = _currentIndex < _allDates.Count - 1 ? _allDates[_currentIndex + 1].ToString("dd/MM/yy") : "";

            UpdateSummary();
        }

        // ── "Add hours" chips section ──────────────────────────────────────────────

        private void BuildAddHoursSection(DateTime date, HashSet<int> scheduled,
            HashSet<int> approved, HashSet<int> pending, bool isToday, int nowHour)
        {
            HourSlotsPanel.Children.Add(new Border
            {
                Height     = 1,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDE7F6")),
                Margin     = new Thickness(0, 12, 0, 10)
            });
            HourSlotsPanel.Children.Add(new TextBlock
            {
                Text       = "➕ הוסף שעות ליום זה",
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A148C")),
                Margin     = new Thickness(0, 0, 0, 8)
            });

            var wrap = new WrapPanel { Orientation = Orientation.Horizontal };

            for (int h = DayStart; h < DayEnd; h++)
            {
                // h == 24 represents the slot 23:00–00:00, displayed as "00:00"
                string label     = h == 24 ? "00:00" : $"{h:D2}:00";
                bool   isTaken   = approved.Contains(h);
                bool   isPending = pending.Contains(h);
                bool   inSched   = scheduled.Contains(h);
                bool   alreadyPast = isToday && h <= nowHour;

                // Taken/pending hours already appear as cells in the main grid — skip them here
                if (isTaken || isPending) continue;

                if (inSched || alreadyPast)
                {
                    string chipText  = alreadyPast ? $"⏰ {label}" : $"✓ {label}";
                    string chipColor = alreadyPast ? "#EEEEEE" : "#EDE7F6";
                    string chipFg    = alreadyPast ? "#BDBDBD" : "#6750A4";
                    string tooltip   = alreadyPast ? "שעה שעברה" : "כבר בלוח הזמנים";
                    wrap.Children.Add(BuildAddChip(h, chipText, chipColor, chipFg,
                        isClickable: false, tooltip: tooltip));
                }
                else
                {
                    _hourStates[h] = SlotState.ToAdd;
                    var chip = BuildAddChip(h, label, "#F1F8E9", "#33691E",
                        isClickable: true, tooltip: "לחץ להוסיף שעה זו");
                    chip.MouseLeftButtonUp += AddChip_Click;
                    wrap.Children.Add(chip);
                }
            }

            HourSlotsPanel.Children.Add(wrap);
        }

        private Border BuildAddChip(int hour, string text, string bg, string fg,
                                    bool isClickable, string tooltip = null)
        {
            var chip = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                CornerRadius = new CornerRadius(20),
                Padding      = new Thickness(12, 6, 12, 6),
                Margin       = new Thickness(0, 0, 6, 6),
                Cursor       = isClickable ? Cursors.Hand : Cursors.Arrow,
                Tag          = hour,
                ToolTip      = tooltip
            };
            chip.Child = new TextBlock
            {
                Text       = text,
                FontSize   = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg))
            };
            return chip;
        }

        private void AddChip_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border chip) return;
            int    hour    = (int)chip.Tag;
            string display = hour == 24 ? "00:00" : $"{hour:D2}:00";
            bool   sel     = _hoursToAdd.Contains(hour);

            if (sel)
            {
                _hoursToAdd.Remove(hour);
                chip.Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F8E9"));
                chip.BorderBrush     = null;
                chip.BorderThickness = new Thickness(0);
                if (chip.Child is TextBlock tb) { tb.Text = display; tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#33691E")); }
            }
            else
            {
                _hoursToAdd.Add(hour);
                chip.Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A5D6A7"));
                chip.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                chip.BorderThickness = new Thickness(1.5);
                if (chip.Child is TextBlock tb) { tb.Text = $"✓ {display}"; tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B5E20")); }
            }

            UpdateSummary();
        }

        // ── Hour cells (existing schedule) ─────────────────────────────────────────

        private Border BuildHourCell(int hour, SlotState state)
        {
            string display = hour == 24 ? "00:00" : $"{hour:D2}:00";

            var (bg, fg, isClickable, label) = state switch
            {
                SlotState.Available        => ("#A5D6A7", "#1B5E20", true,  display),
                SlotState.Taken            => ("#EF9A9A", "#B71C1C", false, $"🔒 {display}"),
                SlotState.PendingApproval  => ("#FFE0B2", "#E65100", false, $"⏳ {display}  ממתין לאישורך"),
                SlotState.MarkedForRemoval => ("#FFCC80", "#E65100", true,  $"✂️ {display}"),
                SlotState.Past             => ("#EEEEEE", "#BDBDBD", false, display),
                _                          => ("#F5F5F5", "#616161", false, display)
            };

            var cell = new Border
            {
                Background   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                CornerRadius = new CornerRadius(12),
                Margin       = new Thickness(0, 0, 0, 6),
                Padding      = new Thickness(20, 12, 20, 12),
                Cursor       = isClickable ? Cursors.Hand : Cursors.Arrow,
                Tag          = hour
            };
            if (state == SlotState.MarkedForRemoval)
            {
                cell.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                cell.BorderThickness = new Thickness(1.5);
            }
            if (state == SlotState.PendingApproval)
            {
                cell.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB74D"));
                cell.BorderThickness = new Thickness(1.5);
            }
            cell.Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.06 };
            cell.Child  = new TextBlock
            {
                Text       = label,
                FontSize   = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg))
            };

            if (isClickable)
                cell.MouseLeftButtonUp += HourCell_Click;

            return cell;
        }

        private void HourCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border cell) return;
            int hour = (int)cell.Tag;

            if (_hourStates[hour] == SlotState.Available)
            {
                _hourStates[hour] = SlotState.MarkedForRemoval;
                _markedForRemoval.Add(hour);
                RefreshCell(cell, SlotState.MarkedForRemoval, hour);
            }
            else if (_hourStates[hour] == SlotState.MarkedForRemoval)
            {
                _hourStates[hour] = SlotState.Available;
                _markedForRemoval.Remove(hour);
                RefreshCell(cell, SlotState.Available, hour);
            }

            UpdateSummary();
        }

        private void RefreshCell(Border cell, SlotState state, int hour)
        {
            string display = hour == 24 ? "00:00" : $"{hour:D2}:00";
            var (bg, fg, label) = state switch
            {
                SlotState.Available        => ("#A5D6A7", "#1B5E20", display),
                SlotState.MarkedForRemoval => ("#FFCC80", "#E65100", $"✂️ {display}"),
                _                          => ("#EEEEEE", "#BDBDBD", display)
            };
            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            if (state == SlotState.MarkedForRemoval)
            { cell.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")); cell.BorderThickness = new Thickness(1.5); }
            else
            { cell.BorderBrush = null; cell.BorderThickness = new Thickness(0); }
            if (cell.Child is TextBlock tb) { tb.Text = label; tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)); }
        }

        // ── Summary bar ────────────────────────────────────────────────────────────

        private void UpdateSummary()
        {
            bool hasRem = _markedForRemoval.Any();
            bool hasAdd = _hoursToAdd.Any();

            if (!hasRem && !hasAdd)
            {
                RemovalSummary.Text      = "לחץ על שעה ירוקה להסרתה  •  בחר שעות בקטע ההוספה";
                RemoveHoursBtn.IsEnabled = false;
                return;
            }

            var parts = new List<string>();
            if (hasRem) parts.Add($"✂️  {_markedForRemoval.Count} שעות להסרה");
            if (hasAdd) parts.Add($"➕  {_hoursToAdd.Count} שעות להוספה");

            RemovalSummary.Text      = string.Join("   |   ", parts);
            RemoveHoursBtn.IsEnabled = true;
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void PrevDate_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentIndex > 0) { _currentIndex--; RenderDay(); }
        }

        private void NextDate_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentIndex < _allDates.Count - 1) { _currentIndex++; RenderDay(); }
        }

        // ── Save changes ──────────────────────────────────────────────────────────

        private async void RemoveHours_Click(object sender, RoutedEventArgs e)
        {
            if (!_markedForRemoval.Any() && !_hoursToAdd.Any()) return;

            var date = _allDates[_currentIndex];

            var lines = new List<string>();
            if (_markedForRemoval.Any())
            {
                var rs = _markedForRemoval.OrderBy(h => h).ToList();
                lines.Add($"הסרה: {rs.Count} שעות  ({HourLabel(rs.First())} – {HourLabel(rs.Last() + 1)})");
            }
            if (_hoursToAdd.Any())
            {
                var ad = _hoursToAdd.OrderBy(h => h).ToList();
                lines.Add($"הוספה: {ad.Count} שעות  ({HourLabel(ad.First())} – {HourLabel(ad.Last() + 1)})");
            }

            if (!CustomDialogHelper.ShowConfirm($"לשמור את השינויים ב-{date:dd/MM/yyyy}?\n\n{string.Join("\n", lines)}", "שמירת שינויים", Window.GetWindow(this))) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var approved = ExpandRequestHours(date, _approvedReqs);
                var pending  = ExpandRequestHours(date, _pendingReqs);
                var original = ExpandSlotHours(date);

                // New desired schedule = (original + toAdd) − removals
                // Never remove a booked (approved/pending) hour
                var bookedHours = new HashSet<int>(approved);
                bookedHours.UnionWith(pending);

                var newSchedule = new HashSet<int>(original);
                newSchedule.UnionWith(_hoursToAdd);
                newSchedule.ExceptWith(_markedForRemoval.Where(h => !bookedHours.Contains(h)));

                // Delete all existing entries for this day and re-insert as blocks
                foreach (var slot in _mySlots.Where(s => s.DateAvailable.Date == date).ToList())
                    await _api.DeleteScheduleAsync(slot.Id);

                foreach (var (blockStart, blockEnd) in BuildContiguousBlocks(newSchedule))
                {
                    await _api.InsertScheduleAsync(new Schedule
                    {
                        BabysitterId  = new BabySitterTeens { Id = _teen.Id },
                        DateAvailable = date,
                        Starttime     = ToTimeOnly(blockStart),
                        Endtime       = ToTimeOnly(blockEnd)
                    });
                }

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                CustomDialogHelper.ShowError("שגיאה בשמירת השינויים: " + ex.Message, Window.GetWindow(this));
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// hour 24 → "00:00",  others → "HH:00"
        private static string HourLabel(int h) => h >= 24 ? "00:00" : $"{h:D2}:00";

        /// Converts internal hour (0-24) to TimeOnly.
        /// Hour 24 = midnight end-of-day → stored as TimeOnly(0,0).
        private static TimeOnly ToTimeOnly(int hour) =>
            hour >= 24 ? new TimeOnly(0, 0) : TimeOnly.FromTimeSpan(TimeSpan.FromHours(hour));

        /// Splits a set of hours into (inclusiveStart, exclusiveEnd) blocks.
        /// e.g. {9,10,11,13,14} → [(9,12),(13,15)]
        private static List<(int start, int end)> BuildContiguousBlocks(HashSet<int> hours)
        {
            var blocks = new List<(int, int)>();
            if (!hours.Any()) return blocks;

            var sorted = hours.OrderBy(h => h).ToList();
            int blockStart = sorted[0], prev = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] != prev + 1) { blocks.Add((blockStart, prev + 1)); blockStart = sorted[i]; }
                prev = sorted[i];
            }
            blocks.Add((blockStart, prev + 1));
            return blocks;
        }

        // ── Empty state ───────────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            HourSlotsPanel.Children.Clear();
            HourSlotsPanel.Children.Add(new TextBlock
            {
                Text = "אין זמינות עתידית. הוסף שעות מהאזור האישי שלך.",
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 40, 0, 0)
            });
            PrevPanel.Visibility = Visibility.Hidden;
            NextPanel.Visibility = Visibility.Hidden;
        }

        // ── Back ──────────────────────────────────────────────────────────────────

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}
