using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BabySitter.Pages
{
    public partial class RateDialog : Window
    {
        public int    SelectedRating { get; private set; } = 0;
        public string SelectedTags   { get; private set; } = "";
        public string ReviewText     { get; private set; } = "";

        private readonly TextBlock[] _stars;
        private static readonly SolidColorBrush Gold = new(Color.FromRgb(255, 193, 7));
        private static readonly SolidColorBrush Gray = new(Color.FromRgb(204, 204, 204));

        private readonly CheckBox[] _tagBoxes;

        public RateDialog(string babysitterName, int existingRating = 0,
                          string existingTags = null, string existingReview = null)
        {
            InitializeComponent();
            TitleText.Text = babysitterName;
            _stars    = new[] { S1, S2, S3, S4, S5 };
            _tagBoxes = new[] { TagReliable, TagPatient, TagCreative, TagPro,
                                TagDedicated, TagNice, TagResp, TagInterest };

            foreach (var star in _stars)
            {
                star.MouseEnter += Star_Hover;
                star.MouseLeave += Star_Leave;
            }

            if (existingRating > 0)
            {
                SelectedRating      = existingRating;
                SubmitBtn.IsEnabled = true;
                SubmitBtn.Content   = "עדכן דירוג";
                for (int i = 0; i < 5; i++)
                    _stars[i].Foreground = i < existingRating ? Gold : Gray;
            }

            if (!string.IsNullOrEmpty(existingTags))
            {
                var saved = existingTags.Split(',').Select(t => t.Trim()).ToHashSet();
                foreach (var cb in _tagBoxes)
                    if (saved.Contains(cb.Content?.ToString()))
                        cb.IsChecked = true;
            }

            if (!string.IsNullOrEmpty(existingReview))
                ReviewTextBox.Text = existingReview;
        }

        private void Star_Hover(object sender, MouseEventArgs e)
        {
            if (sender is not TextBlock tb || !int.TryParse(tb.Tag?.ToString(), out int hovered)) return;
            for (int i = 0; i < 5; i++)
                _stars[i].Foreground = i < hovered ? Gold : Gray;
        }

        private void Star_Leave(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < 5; i++)
                _stars[i].Foreground = i < SelectedRating ? Gold : Gray;
        }

        private void Star_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb || !int.TryParse(tb.Tag?.ToString(), out int rating)) return;
            SelectedRating = rating;
            SubmitBtn.IsEnabled = true;
            for (int i = 0; i < 5; i++)
                _stars[i].Foreground = i < rating ? Gold : Gray;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var tags = _tagBoxes
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content?.ToString())
                .Where(t => !string.IsNullOrEmpty(t));
            SelectedTags = string.Join(",", tags);
            ReviewText   = ReviewTextBox.Text?.Trim() ?? "";
            DialogResult = true;
        }
    }
}
