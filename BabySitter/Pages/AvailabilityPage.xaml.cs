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
    public partial class AvailabilityPage : Page
    {
        private enum SlotState { Available, Taken, Selected, Past }

        private readonly BabySitterTeens _teen;
        private readonly ApiService _api = new ApiService();

        private List<DateTime> _availableDates = new();
        private int _currentDateIndex = 0;
        private readonly HashSet<int> _selectedHours = new();

        // hour → state for the currently displayed day
        private Dictionary<int, SlotState> _hourStates = new();

        private List<BabySitterRate> _allRates = new();

        public AvailabilityPage(BabySitterTeens teen)
        {
            InitializeComponent();
            _teen = teen;
            BindHeader();
            Loaded += async (s, e) => await LoadDataAsync();
        }

        // ── Header ────────────────────────────────────────────────────────────────

        private void BindHeader()
        {
            BabysitterNameLabel.Text  = $"{_teen.FirstName} {_teen.LastName}";
            BabysitterCityLabel.Text  = _teen.CityNameId?.CityName ?? "";
            BabysitterPriceLabel.Text = $"₪{_teen.PriceForAnHour} לשעה";
            BabysitterPhoneLabel.Text = _teen.Telephone ?? "";
            ImageHelper.ApplyAvatar(_teen.ProfilePicture, _teen.FirstName,
                AvatarLetter, AvatarImageEllipse, AvatarImageBrush);

            // Age chip
            if (_teen.DateOfBirth != default)
            {
                int age = DateTime.Today.Year - _teen.DateOfBirth.Year;
                if (_teen.DateOfBirth > DateTime.Today.AddYears(-age)) age--;
                BabysitterAgeLabel.Text = $"גיל {age}";
            }

            // Email chip (only when non-empty)
            if (!string.IsNullOrWhiteSpace(_teen.Mail))
            {
                BabysitterMailLabel.Text  = _teen.Mail;
                MailChip.Visibility       = Visibility.Visible;
            }
        }

        private void UpdateAvgRating()
        {
            var myRates = _allRates.Where(r => r.IdBabySitter?.Id == _teen.Id).ToList();

            var gold  = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            var gray  = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            var stars = new[] { AvgS1, AvgS2, AvgS3, AvgS4, AvgS5 };

            if (myRates.Any())
            {
                double avg   = myRates.Average(r => r.Stars);
                int    round = (int)Math.Round(avg);
                for (int i = 0; i < 5; i++)
                    stars[i].Foreground = i < round ? gold : gray;
                AvgRatingText.Text = $"{avg:F1}  ({myRates.Count} דירוגים)";
            }
            else
            {
                for (int i = 0; i < 5; i++) stars[i].Foreground = gray;
                AvgRatingText.Text = _parentCanRate ? "לחץ לדרג" : "אין דירוג";
            }

            // Always show the chip when there are ratings OR when the current parent can rate
            if (myRates.Any() || _parentCanRate)
                AvgRatingChip.Visibility = Visibility.Visible;
        }

        // ── Data loading ──────────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                var schedulesTask = _api.GetAllSchedulesAsync();
                var requestsTask  = _api.GetAllRequestsAsync();
                var ratesTask     = _api.GetAllBabySitterRatesAsync();
                await System.Threading.Tasks.Task.WhenAll(schedulesTask, requestsTask, ratesTask);

                _allRates = ratesTask.Result?.ToList() ?? new List<BabySitterRate>();
                // UpdateRateButton first — sets _parentCanRate so UpdateAvgRating shows the chip correctly
                UpdateRateButton(requestsTask.Result);
                UpdateAvgRating();

                var mySlots = (schedulesTask.Result ?? Enumerable.Empty<Schedule>())
                    .Where(s => s.BabysitterId?.Id == _teen.Id)
                    .ToList();

                var blockedRequests = (requestsTask.Result ?? Enumerable.Empty<Requests>())
                    .Where(r => r.BabysitterId?.Id == _teen.Id &&
                                (r.Status == "approved" || r.Status == "pending"))
                    .ToList();

                // Build list of dates that have at least one free hour
                _availableDates = BuildAvailableDates(mySlots, blockedRequests);

                if (!_availableDates.Any())
                {
                    ShowNoAvailability();
                    return;
                }

                // Start on the first available date from today onwards
                _currentDateIndex = 0;
                var today = DateTime.Today;
                for (int i = 0; i < _availableDates.Count; i++)
                {
                    if (_availableDates[i].Date >= today) { _currentDateIndex = i; break; }
                }

                // Stash data on the page so we can re-compute without re-fetching
                Tag = (mySlots, blockedRequests);
                RenderDay();
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת זמינות: " + ex.Message);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // ── Date availability logic ───────────────────────────────────────────────

        private List<DateTime> BuildAvailableDates(List<Schedule> slots, List<Requests> blocked)
        {
            var dates = new SortedSet<DateTime>();

            // Include any date that has at least one free hour (parent can still book)
            foreach (var slot in slots)
            {
                var freeHours = GetFreeHours(slot.DateAvailable.Date, new[] { slot }, blocked);
                if (freeHours.Any())
                    dates.Add(slot.DateAvailable.Date);
            }

            // Also include dates that have bookings but no schedule entry —
            // so the parent can always see red "taken" hours on booked days.
            foreach (var req in blocked.Where(r => r.TimeOfRequest.Date >= DateTime.Today))
                dates.Add(req.TimeOfRequest.Date);

            return dates.ToList();
        }

        // Returns hours that are in schedule but NOT blocked and NOT in the past
        private HashSet<int> GetFreeHours(DateTime date, IEnumerable<Schedule> slots, IEnumerable<Requests> blocked)
        {
            // Entire day already passed — nothing available
            if (date.Date < DateTime.Today)
                return new HashSet<int>();

            var scheduled = ExpandSlotHours(date, slots);
            var taken     = ExpandRequestHours(date, blocked);
            scheduled.ExceptWith(taken);

            // Today: remove hours that have already passed
            if (date.Date == DateTime.Today)
                scheduled.RemoveWhere(h => h <= DateTime.Now.Hour);

            return scheduled;
        }

        private HashSet<int> ExpandSlotHours(DateTime date, IEnumerable<Schedule> slots)
        {
            var hours = new HashSet<int>();
            foreach (var s in slots.Where(s => s.DateAvailable.Date == date))
                for (int h = s.Starttime.Hour; h < s.Endtime.Hour; h++)
                    hours.Add(h);
            return hours;
        }

        private HashSet<int> ExpandRequestHours(DateTime date, IEnumerable<Requests> reqs)
        {
            var hours = new HashSet<int>();
            foreach (var r in reqs.Where(r => r.TimeOfRequest.Date == date))
            {
                int len = r.LenghtTime > 0 ? r.LenghtTime : 1;
                for (int h = r.TimeOfRequest.Hour; h < r.TimeOfRequest.Hour + len; h++)
                    hours.Add(h);
            }
            return hours;
        }

        // ── Rendering ─────────────────────────────────────────────────────────────

        private void RenderDay()
        {
            _selectedHours.Clear();
            HourSlotsPanel.Children.Clear();
            _hourStates.Clear();

            var date = _availableDates[_currentDateIndex];
            CurrentDateLabel.Text = date.ToString("dd/MM/yy");

            var (slots, blocked) = ((List<Schedule>, List<Requests>))Tag;

            var scheduledHours = ExpandSlotHours(date, slots);
            var takenHours     = ExpandRequestHours(date, blocked);
            bool isPastDay     = date.Date < DateTime.Today;
            bool isToday       = date.Date == DateTime.Today;
            int  nowHour       = DateTime.Now.Hour;

            // Union: show every hour that is either scheduled OR has a booking.
            // This ensures taken hours that were removed from the schedule still
            // appear in red so the parent can see the babysitter is actually busy.
            var allHours = new HashSet<int>(scheduledHours);
            allHours.UnionWith(takenHours);

            foreach (int h in allHours.OrderBy(x => x))
            {
                SlotState state;
                if (isPastDay || (isToday && h <= nowHour))
                    state = SlotState.Past;
                else if (takenHours.Contains(h))
                    state = SlotState.Taken;
                else
                    state = SlotState.Available;

                _hourStates[h] = state;
                HourSlotsPanel.Children.Add(BuildHourCell(h, state));
            }

            // Prev / next nav labels
            bool hasPrev = _currentDateIndex > 0;
            bool hasNext = _currentDateIndex < _availableDates.Count - 1;

            PrevPanel.Visibility = hasPrev ? Visibility.Visible : Visibility.Hidden;
            NextPanel.Visibility = hasNext ? Visibility.Visible : Visibility.Hidden;
            PrevDateLabel.Text = hasPrev ? _availableDates[_currentDateIndex - 1].ToString("dd/MM/yy") : "";
            NextDateLabel.Text = hasNext ? _availableDates[_currentDateIndex + 1].ToString("dd/MM/yy") : "";

            UpdateSelectionSummary();
        }

        private Border BuildHourCell(int hour, SlotState state)
        {
            var (bg, fg, isClickable) = state switch
            {
                SlotState.Available => ("#A5D6A7", "#1B5E20", true),
                SlotState.Taken     => ("#EF9A9A", "#B71C1C", false),
                SlotState.Selected  => ("#FFF176", "#F57F17", true),
                SlotState.Past      => ("#EEEEEE", "#BDBDBD", false),
                _                   => ("#F5F5F5", "#616161", false)
            };

            var cell = new Border
            {
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                CornerRadius    = new CornerRadius(12),
                Margin          = new Thickness(0, 0, 0, 6),
                Padding         = new Thickness(20, 12, 20, 12),
                Cursor          = isClickable ? Cursors.Hand : Cursors.Arrow,
                Tag             = hour
            };
            if (state == SlotState.Selected)
            {
                cell.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9A825"));
                cell.BorderThickness = new Thickness(1.5);
            }
            cell.Effect = new DropShadowEffect { BlurRadius = 6, ShadowDepth = 1, Opacity = 0.06, Color = Colors.Black };

            var label = new TextBlock
            {
                Text       = $"{hour:D2}:00",
                FontSize   = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg))
            };
            cell.Child = label;

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
                _hourStates[hour] = SlotState.Selected;
                _selectedHours.Add(hour);
                RefreshCell(cell, SlotState.Selected, hour);
            }
            else if (_hourStates[hour] == SlotState.Selected)
            {
                _hourStates[hour] = SlotState.Available;
                _selectedHours.Remove(hour);
                RefreshCell(cell, SlotState.Available, hour);
            }

            UpdateSelectionSummary();
        }

        private void RefreshCell(Border cell, SlotState state, int hour)
        {
            var (bg, fg, _) = state switch
            {
                SlotState.Available => ("#A5D6A7", "#1B5E20", true),
                SlotState.Selected  => ("#FFF176", "#F57F17", true),
                _                   => ("#F5F5F5", "#616161", false)
            };

            cell.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));

            if (state == SlotState.Selected)
            {
                cell.BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9A825"));
                cell.BorderThickness = new Thickness(1.5);
            }
            else
            {
                cell.BorderThickness = new Thickness(0);
            }

            if (cell.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
        }

        private void UpdateSelectionSummary()
        {
            if (!_selectedHours.Any())
            {
                SelectionSummary.Text = "בחר שעות פנויות";
                SendRequestBtn.IsEnabled = false;
                return;
            }

            var sorted = _selectedHours.OrderBy(h => h).ToList();
            string start = $"{sorted.First():D2}:00";
            string end   = $"{sorted.Last() + 1:D2}:00";
            int hours    = sorted.Count;
            SelectionSummary.Text    = $"{start} - {end}  ({hours} שעות)";
            SendRequestBtn.IsEnabled = true;
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void PrevDate_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentDateIndex > 0) { _currentDateIndex--; RenderDay(); }
        }

        private void NextDate_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentDateIndex < _availableDates.Count - 1) { _currentDateIndex++; RenderDay(); }
        }

        // ── Send request ──────────────────────────────────────────────────────────

        private async void SendRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedHours.Any()) return;

            var sorted = _selectedHours.OrderBy(h => h).ToList();

            // Validate consecutive
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] != sorted[i - 1] + 1)
                {
                    MessageBox.Show("יש לבחור שעות עוקבות (רצופות) בלבד.", "שגיאה בבחירה",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var date = _availableDates[_currentDateIndex];
            int startHour = sorted.First();
            int lenghtTime = sorted.Count;

            var req = new Requests
            {
                ParentsId     = LogInComputer.CurrentUser as Parents,
                BabysitterId  = _teen,
                Status        = "pending",
                TimeOfRequest = date.Date.AddHours(startHour),
                LenghtTime    = lenghtTime
            };

            try
            {
                SendRequestBtn.IsEnabled = false;
                await _api.InsertRequestAsync(req);
                MessageBox.Show(
                    $"הבקשה נשלחה!\n{date:dd/MM/yyyy}  {startHour:D2}:00 - {startHour + lenghtTime:D2}:00",
                    "בקשה נשלחה", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.Navigate(new RequestsParents());
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בשליחת הבקשה: " + ex.Message);
                SendRequestBtn.IsEnabled = true;
            }
        }

        // ── Rating ────────────────────────────────────────────────────────────────

        private BabySitterRate _myExistingRate = null;
        private bool           _parentCanRate  = false;

        private void UpdateRateButton(IEnumerable<Requests> allRequests)
        {
            if (LogInComputer.WhoAmI != "parent") return;

            var currentParent = LogInComputer.CurrentUser as Parents;
            if (currentParent == null) return;

            // Consider a job "past" when its end time (start + duration) has already passed
            bool hadPastJob = (allRequests ?? Enumerable.Empty<Requests>()).Any(r =>
                r.BabysitterId?.Id == _teen.Id &&
                r.ParentsId?.Id   == currentParent.Id &&
                r.Status == "approved" &&
                r.TimeOfRequest.AddHours(r.LenghtTime > 0 ? r.LenghtTime : 1) <= DateTime.Now);

            if (!hadPastJob) return;

            _myExistingRate = _allRates.FirstOrDefault(r =>
                r.IdBabySitter?.Id == _teen.Id && r.IdParent?.Id == currentParent.Id);

            _parentCanRate = true;

            // Make the avg chip clickable — one-time subscription guard
            AvgRatingChip.Cursor             = Cursors.Hand;
            AvgRatingChip.MouseLeftButtonUp -= AvgRatingChip_Click; // avoid double-subscribe
            AvgRatingChip.MouseLeftButtonUp += AvgRatingChip_Click;
        }

        // ── Opens RateDialog when parent clicks the rating chip ───────────────────

        private async void AvgRatingChip_Click(object sender, MouseButtonEventArgs e)
        {
            int prefill = _myExistingRate?.Stars ?? 0;
            var dialog  = new RateDialog($"{_teen.FirstName} {_teen.LastName}", prefill)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() != true) return;

            int newStars      = dialog.SelectedRating;
            if (newStars == 0) return;

            var currentParent = LogInComputer.CurrentUser as Parents;

            // ── BabySitterRate ─────────────────────────────────────────────────────
            if (_myExistingRate != null)
            {
                _myExistingRate.Stars      = newStars;
                _myExistingRate.DateOfRate = DateTime.Today;
                await _api.UpdateBabySitterRateAsync(_myExistingRate);
            }
            else
            {
                await _api.InsertBabySitterRateAsync(new BabySitterRate
                {
                    Stars        = newStars,
                    IdBabySitter = _teen,
                    IdParent     = currentParent,
                    DateOfRate   = DateTime.Today
                });
            }

            // Reload rates so future re-rating updates the correct record
            _allRates = (await _api.GetAllBabySitterRatesAsync())?.ToList() ?? _allRates;

            _myExistingRate = _allRates.FirstOrDefault(r =>
                r.IdBabySitter?.Id == _teen.Id && r.IdParent?.Id == currentParent?.Id);

            UpdateAvgRating();
        }

        // ── Empty state ───────────────────────────────────────────────────────────

        private void ShowNoAvailability()
        {
            HourSlotsPanel.Children.Clear();
            HourSlotsPanel.Children.Add(new TextBlock
            {
                Text = "אין שעות זמינות לבייביסיטר זה",
                FontSize = 15, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#79747E")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            });
            PrevPanel.Visibility = Visibility.Hidden;
            NextPanel.Visibility = Visibility.Hidden;
        }
    }
}
