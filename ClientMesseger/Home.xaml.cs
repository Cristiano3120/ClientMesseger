using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, Grid> _chats;

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
            _chats = new();
            ChatsList.SelectionChanged += async (s, args) =>
            {
                var selectedItem = (ListBoxItem)ChatsList.SelectedItem;
                var key = (string)selectedItem.Tag;
                if (_chats.TryGetValue(key, out var grid))
                {
                    grid = _chats[key];
                    PanelChat.Children.Clear();
                    PanelChat.Children.Add(grid);
                }
                else
                {
                    await Task.Delay(1000);
                    grid = _chats[key];
                    PanelChat.Children.Clear();
                    PanelChat.Children.Add(grid);
                }
            };
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

                if (ChatsList.SelectedIndex != -1)
                {
                    var listBoxItem = (ListBoxItem)ChatsList.SelectedItem;
                    var key = (string)listBoxItem.Tag;

                    if (_chats.TryGetValue(key, out var stackPanel))
                    {
                        stackPanel.Visibility = Visibility.Visible;
                    }
                }

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

                slideInAnimation.Completed += (sender, args) =>
                {
                    if (ChatsList.SelectedIndex != -1)
                    {
                        var listBoxItem = (ListBoxItem)ChatsList.SelectedItem;
                        var key = (string)listBoxItem.Tag;

                        if (_chats.TryGetValue(key, out var stackPanel))
                        {
                            stackPanel.Visibility = Visibility.Collapsed;
                        }
                    }
                };
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideInAnimation);
            }
        }

        public void CloseAllPanels()
        {
            var panels = new List<(Grid, TranslateTransform)>()
            {
                (FriendsPanel, FriendsPanelTranslateTransform),
                (PendingFriendRequestsPanel, PendingFriendRequestsTranslateTransform),
                (BlockedPanel, BlockedPanelTranslateTransform),
                (AddFriendsPanel, AddFriendsPanelTranslateTransform),
            };

            foreach (var (grid, translateTransform) in panels)
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

                    if (ChatsList.SelectedIndex != -1)
                    {
                        var listBoxItem = (ListBoxItem)ChatsList.SelectedItem;
                        var key = (string)listBoxItem.Tag;

                        if (_chats.TryGetValue(key, out var stackPanel))
                        {
                            stackPanel.Visibility = Visibility.Visible;
                        }
                    }

                    translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOutAnimation);
                }
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

            PopulateChatList(friendsList);

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

        public void PopulateChatList(List<Friend> friends)
        {
            foreach (var friend in friends)
            {
                var stackPanelChat = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                var ellipseList = new Ellipse
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
                ellipseList.Fill = imageBrush;

                var textBlock = new TextBlock
                {
                    Text = friend.Username,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(10)
                };

                stackPanelChat?.Children.Add(ellipseList);
                stackPanelChat?.Children.Add(textBlock);

                var listBoxItem = new ListBoxItem
                {
                    Content = stackPanelChat,
                    Tag = friend.Username
                };
                ChatsList.Items.Add(listBoxItem);
            }
        }

        public void PopulateChat(Friend friend, List<Message> messages)
        {
            var mainGrid = new Grid
            {
                Margin = new Thickness(10)
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            };

            var chatStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top
            };

            foreach (var (sender, time, content) in messages)
            {
                var outerStackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(10),
                };

                var imageBrush = new ImageBrush()
                {
                    ImageSource = Client.GetBitmapImageFromBase64String(friend.ProfilPic),
                    Stretch = Stretch.UniformToFill,
                };

                var ellipse = new Ellipse
                {
                    Width = 45,
                    Height = 45,
                    Margin = new Thickness(0, 0, 10, 0),
                    Fill = imageBrush
                };

                var innerStackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = 300,
                };

                var nameAndTimePanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };

                var nameTextBlock = new TextBlock
                {
                    Text = sender.Username,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.LightGray,
                    Margin = new Thickness(0, 0, 20, 0)
                };

                var dateTimeTextBlock = new TextBlock
                {
                    Text = time.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                nameAndTimePanel.Children.Add(nameTextBlock);
                nameAndTimePanel.Children.Add(dateTimeTextBlock);

                var messageTextBlock = new TextBlock
                {
                    Text = content,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 0),
                    FontSize = 14,
                    Foreground = Brushes.LightGray
                };

                innerStackPanel.Children.Add(nameAndTimePanel);
                innerStackPanel.Children.Add(messageTextBlock);
                outerStackPanel.Children.Add(ellipse);
                outerStackPanel.Children.Add(innerStackPanel);
                chatStackPanel.Children.Add(outerStackPanel);
            }

            scrollViewer.Content = chatStackPanel;
            Grid.SetRow(scrollViewer, 0);
            mainGrid.Children.Add(scrollViewer);

            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10, 10, 10, 0)
            };

            var textBox = new TextBox
            {
                Width = 300,
                Height = 30,
                Foreground = Brushes.LightGray,
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#403c44")!,
                Margin = new Thickness(0, 0, 10, 0)
            };

            void SendButtonClick(object sender, RoutedEventArgs args)
            {
                _ = DisplayError.LogAsync("Sent Message");
                var listBoxItem = (ListBoxItem)ChatsList.SelectedItem;
                var friendUsername = (string)listBoxItem.Tag;

                var payload = new
                {
                    code = 18,
                    message = textBox.Text,
                    friendUsername = friend.Username,
                    username = Client.Username,
                    time = DateTime.Now,
                };
                var jsonString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(jsonString);

                var message = new Message()
                {
                    Content = textBox.Text,
                    Sender = new UserAfterLogin { Username = Client.Username! },
                    Time = DateTime.Now,
                };
                textBox.Text = string.Empty;
                AddMessage(friend, message);
            }

            var sendButton = new Button
            {
                Content = "Send",
                Width = 100,
                Height = 30,
            };
            sendButton.Click += SendButtonClick;

            textBox.KeyDown += (sender, args) =>
            {
                if (args.Key == System.Windows.Input.Key.Enter)
                {
                    SendButtonClick(new object(), new RoutedEventArgs());
                }
            };

            inputPanel.Children.Add(textBox);
            inputPanel.Children.Add(sendButton);

            Grid.SetRow(inputPanel, 1);
            mainGrid.Children.Add(inputPanel);

            Grid AddFunc(string s)
            {
                return mainGrid;
            }

            Grid UpdateFunc(string s, Grid panel)
            {
                panel.Children.Add(mainGrid);
                return panel;
            }

            _chats.AddOrUpdate(friend.Username, AddFunc, UpdateFunc);
            scrollViewer.ScrollToEnd();
        }

        private static StackPanel CreateMessagePanel(Friend friend, Message message)
        {
            message.Deconstruct(out var sender, out var time, out var content);
            time = time.ToLocalTime();

            var outerStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };

            var imageBrush = new ImageBrush
            {
                Stretch = Stretch.UniformToFill,
            };

            string username;
            if (sender.Username == Client.Username)
            {
                imageBrush.ImageSource = Client.ProfilPicture;
                username = Client.Username;
            }
            else
            {
                imageBrush.ImageSource = Client.GetBitmapImageFromBase64String(friend.ProfilPic);
                username = friend.Username;
            }

            var ellipse = new Ellipse
            {
                Width = 45,
                Height = 45,
                Margin = new Thickness(0, 0, 10, 0),
                Fill = imageBrush
            };

            var innerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Width = 300,
            };

            var nameAndTimePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            var nameTextBlock = new TextBlock
            {
                Text = username,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(0, 0, 20, 0)
            };

            var dateTimeTextBlock = new TextBlock
            {
                Text = time.ToString("dd.MM.yyyy HH:mm"),
                FontSize = 12,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            nameAndTimePanel.Children.Add(nameTextBlock);
            nameAndTimePanel.Children.Add(dateTimeTextBlock);

            var messageTextBlock = new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 14,
                Foreground = Brushes.LightGray
            };

            innerStackPanel.Children.Add(nameAndTimePanel);
            innerStackPanel.Children.Add(messageTextBlock);
            outerStackPanel.Children.Add(ellipse);
            outerStackPanel.Children.Add(innerStackPanel);

            return outerStackPanel;
        }

        public void AddMessage(Friend friend, Message message)
        {
            if (_chats.TryGetValue(friend.Username, out var mainGrid))
            {
                var scrollViewer = (ScrollViewer)mainGrid.Children[0];
                var chatStackPanel = (StackPanel)scrollViewer.Content;
                var newMessagePanel = CreateMessagePanel(friend, message);
                chatStackPanel.Children.Add(newMessagePanel);
                scrollViewer.ScrollToEnd();
            }
            else
            {
                PopulateChat(friend, new List<Message> { message });
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
                    _ = DisplayError.LogAsync(task.ToVerb());

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
