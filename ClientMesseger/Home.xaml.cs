using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace ClientMesseger
{
    public sealed partial class Home : Window
    {
        private readonly List<string> _friendList;

        public Home()
        {
            InitializeComponent();

            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;

            _friendList = Client.AccessFriendlistFile(FileModeEnum.Read) ?? new();
            PopulateFriendsList();
        }

        // Freundesliste befüllen
        private void PopulateFriendsList()
        {
            FriendsList.Items.Clear();

            foreach (var friend in _friendList)
            {
                var textBlock = new TextBlock
                {
                    Text = friend,
                    Foreground = Brushes.White,
                    FontSize = 16,
                    Margin = new Thickness(10)
                };
                FriendsList.Items.Add(textBlock);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FriendsPanel.Width = ActualWidth * 0.75;
        }

        private void ShowFriendsPanel(object sender, RoutedEventArgs e)
        {
            var translateTransform = FriendsPanelTranslateTransform;

            if (FriendsPanel.Visibility == Visibility.Collapsed)
            {
                FriendsPanel.Visibility = Visibility.Visible;

                var slideInAnimation = new DoubleAnimation
                {
                    From = FriendsPanel.Width,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
            }
            else
            {
                var slideOutAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = FriendsPanel.Width,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                slideOutAnimation.Completed += (s, a) =>
                {
                    FriendsPanel.Visibility = Visibility.Collapsed;
                };

                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
            }
        }
    }
}
