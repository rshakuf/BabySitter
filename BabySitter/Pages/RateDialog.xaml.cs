using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BabySitter.Pages
{
    public partial class RateDialog : Window
    {
        public int SelectedRating { get; private set; } = 0;

        private readonly TextBlock[] _stars;
        private static readonly SolidColorBrush Gold = new(Color.FromRgb(255, 193, 7));
        private static readonly SolidColorBrush Gray = new(Color.FromRgb(204, 204, 204));

        public RateDialog(string babysitterName)
        {
            InitializeComponent();
            TitleText.Text = babysitterName;
            _stars = new[] { S1, S2, S3, S4, S5 };

            // Hover preview
            foreach (var star in _stars)
            {
                star.MouseEnter += Star_Hover;
                star.MouseLeave += Star_Leave;
            }
        }

        private void Star_Hover(object sender, MouseEventArgs e)
        {
            if (sender is not TextBlock tb || !int.TryParse(tb.Tag?.ToString(), out int hovered)) return;
            for (int i = 0; i < 5; i++)
                _stars[i].Foreground = i < hovered ? Gold : Gray;
        }

        private void Star_Leave(object sender, MouseEventArgs e)
        {
            // Restore to selected state
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
            DialogResult = true;
        }
    }
}
