using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClientMesseger
{
    public partial class Home : Window
    {
        private StackPanel? _stackPanelPending;
        private StackPanel? _stackPanelFriends;
        private readonly Stopwatch _stopwatch;

        public Home()
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            ProfilPic.ImageSource = Client.ProfilPicture;
            UsernameText.Text = Client.Username;
            _stopwatch = new Stopwatch();
        }

        public void OnProfilPicChanged(BitmapImage image)
        {
            ProfilPic.ImageSource = image;
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
        public void PopulateFriendsList()
        {
            FriendsList.Items.Clear();
            _stackPanelFriends?.Children.Clear();

            lock (Client.friendsLock)
            {
                foreach (var (friendUsername, friendId, profilPic) in Client.friendList)
                {
                    _stackPanelFriends = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5)
                    };

                    var ellipse = new Ellipse
                    {
                        Width = 45,
                        Height = 45,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };

                    var imageBrush = new ImageBrush()
                    {
                        ImageSource = Client.GetBitmapImageFromBase64String(profilPic),
                        Stretch = Stretch.UniformToFill,
                    };

                    ellipse.Fill = imageBrush;

                    var textBlockUsername = new TextBlock
                    {
                        Text = friendUsername,
                        Foreground = Brushes.White,
                        FontSize = 18,
                        Margin = new Thickness(10)
                    };

                    var blockButton = new Button
                    {
                        Content = "Block",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                        Foreground = Brushes.White,
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5),
                        Tag = friendUsername,
                    };

                    var deleteButton = new Button
                    {
                        Content = "Delete",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0343c")),
                        Foreground = Brushes.White,
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5),
                        Tag = friendUsername,
                    };
                    blockButton.Click += BlockButton_Click;
                    deleteButton.Click += DeleteButton_Click;
                    _stackPanelFriends.Children.Add(ellipse);
                    _stackPanelFriends.Children.Add(textBlockUsername);
                    _stackPanelFriends.Children.Add(blockButton);
                    _stackPanelFriends.Children.Add(deleteButton);
                    FriendsList.Items.Add(_stackPanelFriends);
                }
            }
        }

        //Ausstehende anfragens liste befüllen
        public void PopulatePendingFriendRequestsList()
        {
            PendingFriendsList.Items.Clear();
            _stackPanelPending?.Children?.Clear();

            lock (Client.pendingLock)
            {
                foreach (var pending in Client.pendingFriendRequestsList)
                {
                    _stackPanelPending = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5)
                    };

                    var ellipse = new Ellipse
                    {
                        Width = 45,
                        Height = 45,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0),
                    };

                    var imageBrush = new ImageBrush()
                    {
                        ImageSource = Client.GetBitmapImageFromBase64String(pending.Item3),
                        Stretch = Stretch.UniformToFill,
                    };
                    ellipse.Fill = imageBrush;

                    var textBlock = new TextBlock
                    {
                        Text = pending.Item1,
                        Foreground = Brushes.White,
                        FontSize = 18,
                        Margin = new Thickness(10)
                    };

                    var acceptButton = new Button
                    {
                        Content = "Accept",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#288444")),
                        Foreground = Brushes.White,
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5),
                        Tag = pending.Item1
                    };
                    acceptButton.Click += AcceptButton_Click;

                    var declineButton = new Button
                    {
                        Content = "Decline",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038")),
                        Foreground = Brushes.White,
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5),
                        Tag = pending.Item1
                    };
                    declineButton.Click += DeclineButton_Click;

                    var blockButton = new Button
                    {
                        Content = "Block",
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                        Foreground = Brushes.White,
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(5),
                        Tag = pending.Item1,
                    };
                    blockButton.Click += BlockButton_Click;
                    _stackPanelPending.Children.Add(ellipse);
                    _stackPanelPending.Children.Add(textBlock);
                    _stackPanelPending.Children.Add(acceptButton);
                    _stackPanelPending.Children.Add(declineButton);
                    _stackPanelPending.Children.Add(blockButton);
                    PendingFriendsList.Items.Add(_stackPanelPending);
                }
            }
        }

        #region Buttons_Click

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;

            var friendRequest = Client.pendingFriendRequestsList.Find(x => x.Item1 == username);

            var friendId = friendRequest.Item2;
            lock (Client.friendsLock)
            {
                Client.friendList.Add((username!, friendId, friendRequest.Item3));
            }
            PopulateFriendsList();

            lock (Client.pendingLock)
            {
                Client.pendingFriendRequestsList.Remove(friendRequest);
            }
            PopulatePendingFriendRequestsList();

            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendId = friendRequest.Item2,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Accepted,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;
            (string, int, string) friendRequest;
            lock (Client.pendingLock)
            {
                friendRequest = Client.pendingFriendRequestsList.Find(x => x.Item1 == username);
                Client.pendingFriendRequestsList.Remove(friendRequest);
            }

            PopulatePendingFriendRequestsList();
            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendId = friendRequest.Item2,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Decline,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
        }

        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;
            (string, int, string) friendRequest;
            lock (Client.friendsLock)
            {
                friendRequest = Client.friendList.Find(x => x.Item1 == username);
                Client.friendList.Remove(friendRequest);
            }

            lock (Client.pendingLock)
            {
                Client.pendingFriendRequestsList.Remove(friendRequest);
            }

            PopulateFriendsList();
            PopulatePendingFriendRequestsList();
            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendId = friendRequest.Item2,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Blocked,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;
            (string, int, string) friendRequest;
            lock (Client.friendsLock)
            {
                friendRequest = Client.friendList.Find(x => x.Item1 == username);
                Client.friendList.Remove(friendRequest);
            }

            PopulateFriendsList();
            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendId = friendRequest.Item2,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Delete,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
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
            var usernameSender = Client.Username;
            if (usernameSender == usernameReceiver)
            {
                _ = SetAddFriendText("You can´t add urself :(", Brushes.Red);
                return;
            }

            lock (Client.pendingLock)
            {
                if (Client.pendingFriendRequestsList.Any(x => x.Item1 == usernameReceiver))
                {
                    _ = SetAddFriendText("You already have a pending request from that person", Brushes.Red);
                    return;
                }
            }

            lock (Client.friendsLock)
            {
                if (Client.friendList.Any((x) => x.Item1 == usernameReceiver))
                {
                    _ = SetAddFriendText("You already have that person added", Brushes.Red);
                    return;
                }
            }

            lock (Client.blockedLock)
            {
                if (Client.blockedList.Any(x => x.Item1 == usernameReceiver))
                {
                    _ = SetAddFriendText("You blocked or are blocked by that person", Brushes.Red);
                    return;
                }
            }

            var result = ClientUI.CheckIfCanSendRequest(_stopwatch, 1.5f);
            if (result == 0 || result == -1)
            {
                _stopwatch.Restart();
                var payload = new
                {
                    code = 10,
                    usernameReceiver,
                    usernameSender,
                    senderId = Client.Id,
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

        private void UsernameText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //Wenn dieses Fenster maximized ist und man dann in die Settings geht klappt das von der Location her nicht
            //deswegen dieses if
            Settings settings;
            if (WindowState == WindowState.Maximized)
            {
                settings = new Settings(this)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Maximized,
                };
            }
            else
            {
                settings = new Settings(this)
                {
                    Width = Width,
                    Height = Height,
                    Left = Left,
                    Top = Top,
                };
            }
            settings.Show();
            Hide();
        }

        #endregion
    }
}
