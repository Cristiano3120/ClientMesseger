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
        private StackPanel? _stackPanelBlocked;
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

        public void CloseOrOpenPanel(Grid grid, TranslateTransform translateTransform)
        {
            if (grid.Visibility == Visibility.Visible)
            {
                var slideOutAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = grid.Width,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                slideOutAnimation.Completed += (s, a) =>
                {
                    grid.Visibility = Visibility.Collapsed;
                };
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
            }
            else
            {
                BlockedPanel.Visibility = Visibility.Collapsed;
                AddFriendsPanel.Visibility = Visibility.Collapsed;
                FriendsPanel.Visibility = Visibility.Collapsed;
                PendingFriendRequestsPanel.Visibility = Visibility.Collapsed;

                grid.Visibility = Visibility.Visible;
                var slideInAnimation = new DoubleAnimation
                {
                    From = grid.Width,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3)
                };
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
            }
        }

        #region SetAddFriendText

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

        #endregion

        #region PopulateLists

        public void PopulateFriendsList()
        {
            FriendsList.Items.Clear();
            _stackPanelFriends?.Children.Clear();

            List<Friend> friendsList;
            lock (Client.relationshipStateLock)
            {
                friendsList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Accepted).ToList();
            }

            foreach (var friend in friendsList)
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
                    ImageSource = Client.GetBitmapImageFromBase64String(friend.ProfilPic),
                    Stretch = Stretch.UniformToFill,
                };

                ellipse.Fill = imageBrush;

                var textBlockUsername = new TextBlock
                {
                    Text = friend.Username,
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
                    Tag = friend.Username,
                };

                var deleteButton = new Button
                {
                    Content = "Delete",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0343c")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Tag = friend.Username,
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

        public void PopulatePendingFriendRequestsList()
        {
            PendingFriendsList.Items.Clear();
            _stackPanelPending?.Children.Clear();

            List<Friend> pendingList;
            lock (Client.relationshipStateLock)
            {
                pendingList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Pending).ToList();
            }

            foreach (var pending in pendingList)
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
                    ImageSource = Client.GetBitmapImageFromBase64String(pending.ProfilPic),
                    Stretch = Stretch.UniformToFill,
                };
                ellipse.Fill = imageBrush;

                var textBlock = new TextBlock
                {
                    Text = pending.Username,
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
                    Tag = pending.Username
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
                    Tag = pending.Username
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
                    Tag = pending.Username,
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

        public void PopulateBlockedList()
        {
            BlockedList.Items.Clear();
            _stackPanelBlocked?.Children.Clear();

            List<Friend> blockedList;
            lock (Client.relationshipStateLock)
            {
                blockedList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Blocked).ToList();
                blockedList.Add(new Friend() { ProfilPic = "", Status = RelationshipStateEnum.Blocked, Username = "dhjfc" });
            }

            foreach (var blocked in blockedList)
            {
                _stackPanelBlocked = new StackPanel
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
                    ImageSource = Client.GetBitmapImageFromBase64String(blocked.ProfilPic),
                    Stretch = Stretch.UniformToFill,
                };
                ellipse.Fill = imageBrush;

                var textBlock = new TextBlock
                {
                    Text = blocked.Username,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(10)
                };

                var blockButton = new Button
                {
                    Content = "Unblock",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Tag = blocked.Username,
                };
                blockButton.Click += UnblockButton_Click;
                _stackPanelBlocked?.Children.Add(ellipse);
                _stackPanelBlocked?.Children.Add(textBlock);
                _stackPanelBlocked?.Children.Add(blockButton);
                PendingFriendsList.Items.Add(_stackPanelBlocked);
            }
        }

        #endregion

        #region Buttons_Click

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;

            Friend? friendRequest;
            lock (Client.relationshipStateLock)
            {
                friendRequest = Client.relationshipState.Find(x => x.Username == username);
            }

            friendRequest!.Status = RelationshipStateEnum.Accepted;

            PopulateFriendsList();
            PopulatePendingFriendRequestsList();

            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
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

            lock (Client.relationshipStateLock)
            {
                var friendRequest = Client.relationshipState.Find(x => x.Username == username);
                Client.relationshipState.Remove(friendRequest!);
            }

            PopulatePendingFriendRequestsList();
            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
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
            Friend? friend;

            lock (Client.relationshipStateLock)
            {
                friend = Client.relationshipState.Find(x => x.Username == username);
                friend!.Status = RelationshipStateEnum.Blocked;
            }

            PopulateFriendsList();
            PopulatePendingFriendRequestsList();

            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
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
            Friend? friend;

            lock (Client.relationshipStateLock)
            {
                friend = Client.relationshipState.Find(x => x.Username == username);
                Client.relationshipState.Remove(friend!);
            }

            PopulateFriendsList();

            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Delete,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
        }

        private void UnblockButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var username = button!.Tag as string;
            Friend? friend;

            lock (Client.relationshipStateLock)
            {
                friend = Client.relationshipState.Find(x => x.Username == username);
                Client.relationshipState.Remove(friend!);
            }

            PopulateBlockedList();

            var payload = new
            {
                code = 14,
                username = Client.Username,
                userId = Client.Id,
                friendUsername = username!,
                task = (byte)RelationshipStateEnum.Unblocked,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
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

            Friend? friend;
            lock (Client.relationshipStateLock)
            {
                friend = Client.relationshipState.Find(x => x.Username == usernameReceiver);
            }

            if (friend != null)
            {
                switch (friend.Status)
                {
                    case RelationshipStateEnum.Accepted:
                        _ = SetAddFriendText("You already have that person added", Brushes.Red);
                        break;
                    case RelationshipStateEnum.Blocked:
                        _ = SetAddFriendText("You blocked or are blocked by that person", Brushes.Red);
                        break;
                    case RelationshipStateEnum.Pending:
                        _ = SetAddFriendText("You already have a pending request from that person", Brushes.Red);
                        break;
                }
                return;
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

        private void ShowPendingFriendRequests(object sender, RoutedEventArgs args)
        {
            var translateTransform = PendingFriendRequestsTranslateTransform;  
            CloseOrOpenPanel(PendingFriendRequestsPanel, translateTransform);
        }

        private void ShowFriendsPanel(object sender, RoutedEventArgs args)
        {
            var translateTransform = FriendsPanelTranslateTransform;
            CloseOrOpenPanel(FriendsPanel, translateTransform);
        }

        private void ShowAddFriendPanel(object sender, RoutedEventArgs args)
        {
            var translateTransform = AddFriendsPanelTranslateTransform;
            CloseOrOpenPanel(AddFriendsPanel, translateTransform);
        }

        private void ShowBlockedPanel(object sender, RoutedEventArgs args)
        {
            var translateTransform = BlockedPanelTranslateTransform;
            CloseOrOpenPanel(BlockedPanel, translateTransform);
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
