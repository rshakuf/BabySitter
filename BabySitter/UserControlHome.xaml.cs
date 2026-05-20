using BabySitter.Helpers;
using BabySitter.Pages;
using BabySitter.UserControls;
using Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace BabySitter.UserControls
{
    public partial class UserControlHome : UserControl
    {
        // ── Rating DependencyProperties ───────────────────────────────────────────

        public static readonly DependencyProperty AverageRatingProperty =
            DependencyProperty.Register(nameof(AverageRating), typeof(double), typeof(UserControlHome),
                new PropertyMetadata(0.0, OnRatingChanged));

        public double AverageRating
        {
            get => (double)GetValue(AverageRatingProperty);
            set => SetValue(AverageRatingProperty, value);
        }

        public static readonly DependencyProperty RatingCountProperty =
            DependencyProperty.Register(nameof(RatingCount), typeof(int), typeof(UserControlHome),
                new PropertyMetadata(0, OnRatingChanged));

        public int RatingCount
        {
            get => (int)GetValue(RatingCountProperty);
            set => SetValue(RatingCountProperty, value);
        }

        private static void OnRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as UserControlHome)?.UpdateStars();

        // ─────────────────────────────────────────────────────────────────────────

        public UserControlHome()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => ApplyAvatar();
            Loaded += (s, e) => { ApplyAvatar(); UpdateStars(); };
        }

        private void ApplyAvatar()
        {
            if (DataContext is not BabySitterTeens teen) return;
            ImageHelper.ApplyAvatar(teen.ProfilePicture, teen.FirstName,
                CardAvatarLetter, CardAvatarImage, CardAvatarBrush);
        }

        private void UpdateStars()
        {
            var gold = new SolidColorBrush(Color.FromRgb(255, 193, 7));
            var gray = new SolidColorBrush(Color.FromRgb(204, 204, 204));
            var stars = new[] { Star1, Star2, Star3, Star4, Star5 };
            int filled = (int)Math.Round(AverageRating);

            for (int i = 0; i < 5; i++)
                stars[i].Foreground = i < filled ? gold : gray;

            RatingText.Text = RatingCount == 0
                ? "אין דירוג"
                : $"{AverageRating:F1}  ({RatingCount} דירוגים)";
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            var teen = (sender as Button)?.DataContext as BabySitterTeens;
            if (teen == null) return;

            var nav = NavigationService.GetNavigationService(this);
            if (nav != null)
            {
                if (LogInComputer.WhoAmI == "parent")
                    nav.Navigate(new AvailabilityPage(teen));
                else
                    nav.Navigate(new BabySitterDetailsControl(teen));
            }
        }
    }
}
