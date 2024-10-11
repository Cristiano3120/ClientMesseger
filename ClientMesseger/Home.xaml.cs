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

            List<Friend> friendsList;
            lock (Client.relationshipStateLock)
            {
                friendsList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Accepted).ToList();
            }

            PopulateChats(friendsList);

            foreach (var friend in friendsList)
            {
                var stackPanelFriends = new StackPanel
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
                    Tag = (friend.Username, RelationshipStateEnum.Blocked),
                };

                var deleteButton = new Button
                {
                    Content = "Delete",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0343c")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Tag = (friend.Username, RelationshipStateEnum.Delete),
                };
                blockButton.Click += RelationshipStateChange_Click;
                deleteButton.Click += RelationshipStateChange_Click;
                stackPanelFriends.Children.Add(ellipse);
                stackPanelFriends.Children.Add(textBlockUsername);
                stackPanelFriends.Children.Add(blockButton);
                stackPanelFriends.Children.Add(deleteButton);
                FriendsList.Items.Add(stackPanelFriends);
            }
        }

        public void PopulatePendingFriendRequestsList()
        {
            PendingFriendsList.Items.Clear();

            List<Friend> pendingList;
            lock (Client.relationshipStateLock)
            {
                pendingList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Pending).ToList();
            }

            foreach (var pending in pendingList)
            {
                var stackPanelPending = new StackPanel
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
                    Tag = (pending.Username, RelationshipStateEnum.Accepted)
                };
                acceptButton.Click += RelationshipStateChange_Click;

                var declineButton = new Button
                {
                    Content = "Decline",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44038")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Tag = (pending.Username, RelationshipStateEnum.Decline)
                };
                declineButton.Click += RelationshipStateChange_Click;

                var blockButton = new Button
                {
                    Content = "Block",
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#302c34")),
                    Foreground = Brushes.White,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Tag = (pending.Username, RelationshipStateEnum.Blocked),
                };
                blockButton.Click += RelationshipStateChange_Click;
                stackPanelPending.Children.Add(ellipse);
                stackPanelPending.Children.Add(textBlock);
                stackPanelPending.Children.Add(acceptButton);
                stackPanelPending.Children.Add(declineButton);
                stackPanelPending.Children.Add(blockButton);
                PendingFriendsList.Items.Add(stackPanelPending);
            }
        }

        public void PopulateBlockedList()
        {
            BlockedList.Items.Clear();

            List<Friend> blockedList;
            lock (Client.relationshipStateLock)
            {
                blockedList = Client.relationshipState.Where(x => x.Status == RelationshipStateEnum.Blocked).ToList();
            }

            foreach (var blocked in blockedList)
            {
                var stackPanelBlocked = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5),
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
                    Tag = (blocked.Username, RelationshipStateEnum.Unblocked) 
                };
                blockButton.Click += RelationshipStateChange_Click;
                stackPanelBlocked?.Children.Add(ellipse);
                stackPanelBlocked?.Children.Add(textBlock);
                stackPanelBlocked?.Children.Add(blockButton);
                BlockedList.Items.Add(stackPanelBlocked);
            }
        }

        public void PopulateChats(List<Friend> friends)
        {
            foreach (var friend in friends)
            {
                var stackPanelChats = new StackPanel
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
                    ImageSource = Client.GetBitmapImageFromBase64String(friend.ProfilPic),
                    Stretch = Stretch.UniformToFill,
                };
                ellipse.Fill = imageBrush;

                var textBlock = new TextBlock
                {
                    Text = friend.Username,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(10)
                };

                stackPanelChats?.Children.Add(ellipse);
                stackPanelChats?.Children.Add(textBlock);
                ChatsList.Items.Add(stackPanelChats);
            }
        }

        #endregion

        #region Buttons_Click

        public void RelationshipStateChange_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                if (sender is Button button && button.Tag is (string username, RelationshipStateEnum task))
                {
                    Friend? relation;
                    Console.WriteLine(task.ToVerb());

                    lock (Client.relationshipStateLock)
                    {
                        relation = Client.relationshipState.Find(x => x.Username == username) 
                            ?? throw new Exception("Error while trying to interact with that user.");

                        if (task == RelationshipStateEnum.Accepted || task == RelationshipStateEnum.Blocked)
                        {
                            relation.Status = task;
                        }
                        else
                        {
                            Client.relationshipState.Remove(relation);
                        }
                    }

                    PopulateFriendsList();
                    PopulatePendingFriendRequestsList();
                    PopulateBlockedList();

                    var payload = new
                    {
                        code = 14,
                        username = Client.Username,
                        userId = Client.Id,
                        friendUsername = username,
                        task,
                    };
                    var jsonString = JsonSerializer.Serialize(payload);
                    _ = Client.SendPayloadAsync(jsonString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                DisplayError.DisplayBasicErrorInfos(ex, "Home.xaml.cs", "RelationshipStateChange_Click");
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
