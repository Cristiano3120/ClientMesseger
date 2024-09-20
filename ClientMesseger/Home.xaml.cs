using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;

namespace ClientMesseger
{
    public partial class Home : Window
    {
        private List<string>? _friendList;
        private List<string>? _pendingFriendRequestsList;
        private readonly Stopwatch _stopwatch;

        public Home()
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            UsernameText.Text = Client._username;
            _stopwatch = new Stopwatch();
            PopulateFriendsList();
            PopulatePendingFriendRequestsList();
        }

        public async Task SetAddFriendText(string input, Brush color)
        {
            friendNameTextBlock.Foreground = color;
            friendNameTextBlock.Text = input;
            await Task.Delay(3000);
            friendNameTextBlock.Foreground = Brushes.White;
            friendNameTextBlock.Text = "Enter a Username";
        }

        public async Task SetAddFriendText(string input, Brush color, int delay)
        {
            friendNameTextBlock.Foreground = color;
            friendNameTextBlock.Text = input;
            await Task.Delay(delay);
            friendNameTextBlock.Foreground = Brushes.White;
            friendNameTextBlock.Text = "Enter a Username";
        }

        // Freundesliste befüllen
        private void PopulateFriendsList()
        {
            _friendList = Client.AccessFile(Client.friendlistFile, FileModeEnum.Read) ?? new();
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

        //Ausstehende anfragens liste befüllen
        public void PopulatePendingFriendRequestsList()
        {
            _pendingFriendRequestsList = Client.AccessFile(Client.pendingFriendRequests, FileModeEnum.Read) ?? new();
            PendingFriendsList.Items.Clear();
            foreach (var pending in _pendingFriendRequestsList)
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                var textBlock = new TextBlock
                {
                    Text = pending,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(10, 0, 10, 0)
                };

                var acceptButton = new Button
                {
                    Content = "Accept",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#288444")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Margin = new Thickness(5),
                    Tag = pending
                }; 
                acceptButton.Click += AcceptButton_Click;

                var declineButton = new Button
                {
                    Content = "Decline",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Margin = new Thickness(5),
                    Tag = pending
                };
                declineButton.Click += DeclineButton_Click;

                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(acceptButton);
                stackPanel.Children.Add(declineButton);
                PendingFriendsList.Items.Add(stackPanel);
            }
        }

        #region Buttons_Click

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button is not null)
            {
                var username = button.Tag as string;
                Client.AccessFile(Client.friendlistFile, FileModeEnum.Write, username!);
                Client.AccessFile(Client.pendingFriendRequests, FileModeEnum.DeleteLine, username!);
                PopulateFriendsList();
                PopulatePendingFriendRequestsList();
            }
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button is not null)
            {
                var username = button.Tag as string;
                Client.AccessFile(Client.pendingFriendRequests, FileModeEnum.DeleteLine, username!);
                PopulatePendingFriendRequestsList();
            }
        }

        private void ShowPendingFriendRequests(object sender, RoutedEventArgs args)
        {
            var translateTransform = PendingFriendRequestsTranslateTransform;

            if (AddFriendsPanel.Visibility == Visibility.Visible)
            {
                AddFriendsPanel.Visibility = Visibility.Collapsed;
            }

            if (FriendsPanel.Visibility == Visibility.Visible)
            {
                FriendsPanel.Visibility = Visibility.Collapsed;
            }

            if (PendingFriendRequestsPanel.Visibility == Visibility.Collapsed)
            {
                PendingFriendRequestsPanel.Visibility = Visibility.Visible;
                var slideInAnimation = new DoubleAnimation
                {
                    From = PendingFriendRequestsPanel.Width,
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
                    To = PendingFriendRequestsPanel.Width,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                slideOutAnimation.Completed += (s, a) =>
                {
                    PendingFriendRequestsPanel.Visibility = Visibility.Collapsed;
                };

                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
            }
        }

        private void OnFriendAdded_Click(object sender, RoutedEventArgs args)
        {
            var usernameReceiver = friendNameTextBox.Text;
            var usernameSender = Client._username;
            if (usernameSender == usernameReceiver)
            {
                _ = SetAddFriendText("You can´t add urself :(", Brushes.Red);
                return;
            }
            var result = ClientUI.CheckIfCanSendRequest(_stopwatch, 1.5f);
            _stopwatch.Restart();
            if (result == 0 || result == -1)
            {
                var payload = new
                {
                    code = 10,
                    usernameReceiver,
                    usernameSender
                };
                var jsonString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(jsonString);
            }
            else
            {
                _ = SetAddFriendText($"You have to wait a second. Don´t spam:(", Brushes.White, 1500);
            }
        }

        private void ShowFriendsPanel(object sender, RoutedEventArgs args)
        {
            var translateTransform = FriendsPanelTranslateTransform;
            if (AddFriendsPanel.Visibility == Visibility.Visible)
            {
                AddFriendsPanel.Visibility = Visibility.Collapsed;
            }

            if (PendingFriendRequestsPanel.Visibility == Visibility.Visible)
            {
                PendingFriendRequestsPanel.Visibility = Visibility.Collapsed;
            }

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

        private void ShowAddFriendPanel(object sender, RoutedEventArgs args)
        {
            var translateTransform = AddFriendsPanelTranslateTransform;
            if (FriendsPanel.Visibility == Visibility.Visible)
            {
                FriendsPanel.Visibility = Visibility.Collapsed;
            }

            if (PendingFriendRequestsPanel.Visibility == Visibility.Visible)
            {
                PendingFriendRequestsPanel.Visibility = Visibility.Collapsed;
            }

            if (AddFriendsPanel.Visibility == Visibility.Collapsed)
            {
                AddFriendsPanel.Visibility = Visibility.Visible;

                var slideInAnimation = new DoubleAnimation
                {
                    From = AddFriendsPanel.Width,
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
                    To = AddFriendsPanel.Width,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                slideOutAnimation.Completed += (s, a) =>
                {
                    AddFriendsPanel.Visibility = Visibility.Collapsed;
                };
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
            }
        }

        #endregion

        //musst überlegen ob du das drin lässt
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //FriendsPanel.Width = ActualWidth * 0.75;
            //AddFriendsPanel.Width = ActualWidth * 0.75;
            //PendingFriendRequestsPanel.Width = ActualWidth * 0.75;
        }
    }
}
