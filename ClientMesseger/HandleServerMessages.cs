using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientMesseger
{
    internal class HandleServerMessages
    {
        //Code 7
        public static void FeedbackAccountCreation(JsonElement root)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var result = root.GetProperty("result").GetBoolean();
                if (result == false)
                {
                    var createAccWindow = new CreateAccount("Something went wrong while trying to create an Account. Try again!");
                    createAccWindow.Show();
                    ClientUI.CloseAllWindowsExceptOne(createAccWindow);
                }
                else
                {
                    var email = root.GetProperty("Email").GetString();
                    var password = root.GetProperty("Password").GetString();
                    var username = root.GetProperty("Username").GetString();
                    var profilPic = root.GetProperty("profilPic").GetString();
                    Client.Username = username;
                    Client.ProfilPicture = Client.GetBitmapImageFromBase64String(profilPic!)!;
                    if (Client.ProfilPicture == null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Application.Current.Shutdown();
                        });
                    }
                    WriteLoginDataIntoFile(email!, password!);
                    var home = new Home();
                    home.Show();
                    ClientUI.CloseAllWindowsExceptOne(home);
                }
            });
        }

        //Code 9
        public static void FeedbackLogin(JsonElement root)
        {
            var result = root.GetProperty("result");
            if (result.ValueKind == JsonValueKind.Null)
            {
                _ = DisplayError.Log("Server couldnt connect to the database.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var login = ClientUI.GetWindow<Login>();
                    login?.CallErrorBox("The request can´t be send right now. Try again later.");
                });
                return;
            }
            var canLogin = root.GetProperty("result").GetBoolean();
            if (canLogin)
            {
                var email = root.GetProperty("email").GetString();
                var password = root.GetProperty("password").GetString();
                Client.Username = root.GetProperty("username").GetString();
                Client.Id = root.GetProperty("id").GetInt32();
                var imageBytes = root.GetProperty("profilPic").GetBytesFromBase64();

                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
                Client.ProfilPicture = bitmap;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var home = new Home();
                    home = ClientUI.GetWindow<Home>()!;
                    home.OnProfilPicChanged(Client.ProfilPicture);
                    home.Show();
                    ClientUI.CloseAllWindowsExceptOne(home);
                });

                WriteLoginDataIntoFile(email!, password!);
            }
            else
            {
                _ = Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var login = ClientUI.GetWindow<Login>();
                    var error = canLogin == false ? "Email or password was wrong" : "An error accoured while processing ur request. Try again!";
                    _ = login!.CallErrorBox(error);
                });
            }
        }

        //Code 11
        public static void FeedbackSentFriendrequest(JsonElement root)
        {
            var result = root.GetProperty("result");
            Application.Current.Dispatcher.Invoke(() =>
            {
                var home = ClientUI.GetWindow<Home>();
                if (result.ValueKind == JsonValueKind.Null)
                {
                    _ = home!.SetAddFriendText("Something went wrong! Try again later.", Brushes.White);
                }
                else if (result.GetBoolean() == false)
                {
                    _ = home!.SetAddFriendText("The enterd user couldn´t be found!", Brushes.Red);
                }
                else
                {
                    _ = home!.SetAddFriendText("Sent a friend request!", Brushes.Green);
                }
            });
        }

        //Code 12
        public static void ReceiveFriendrequest(JsonElement root)
        {
            var username = root.GetProperty("usernameSender").GetString();
            var senderId = root.GetProperty("senderId").GetInt32();
            var profilPic = root.GetProperty("profilPic").GetString();
            _ = DisplayError.Log($"{username} added you!");
            lock (Client.pendingLock)
            {
                Client.pendingFriendRequestsList.Add((username!, senderId, profilPic!));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var home = ClientUI.GetWindow<Home>();
                home?.PopulatePendingFriendRequestsList();
            });
        }

        //Code 13
        public static void ReceiveRelationshipStates(JsonElement root)
        {
            Console.WriteLine(root.ToString());
            if (root.TryGetProperty("friends", out var friendsElement) && friendsElement.ValueKind == JsonValueKind.Array)
            {
                var friendsList = JsonSerializer.Deserialize<List<Friend>>(friendsElement.GetRawText());
                foreach (var friend in friendsList!)
                {
                    if (friend.Status == RelationshipStateEnum.Pending)
                    {
                        _ = DisplayError.Log(RelationshipStateEnum.Pending);
                        lock (Client.pendingLock)
                        {
                            Client.pendingFriendRequestsList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
                        }
                    }
                    else if (friend.Status == RelationshipStateEnum.Accepted)
                    {
                        _ = DisplayError.Log(RelationshipStateEnum.Accepted);
                        lock (Client.friendsLock)
                        {
                            Client.friendList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
                        }
                    }
                    else if (friend.Status == RelationshipStateEnum.Blocked)
                    {
                        _ = DisplayError.Log("BLOCKED");
                        lock (Client.blockedLock)
                        {
                            Client.blockedList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
                        }
                    }
                    else
                    {
                        _ = DisplayError.Log("An unknown friendship status was received");
                    }
                }
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                var home = ClientUI.GetWindow<Home>();
                home?.PopulatePendingFriendRequestsList();
                home?.PopulateFriendsList();
            });
        }

        //Code 17
        public static void UpdateRelationship(JsonElement root)
        {
            var sendingUsername = root.GetProperty("username").GetString();
            var sendingId = root.GetProperty("userId").GetInt32();
            var sendingProfilPic = root.GetProperty("profilPic").GetString();
            var task = (RelationshipStateEnum)root.GetProperty("taskByte").GetByte();

            if (task == RelationshipStateEnum.Blocked)
            {
                lock (Client.friendsLock)
                {
                    Client.friendList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                }
                lock (Client.blockedLock)
                {
                    Client.blockedList.Add((sendingUsername!, sendingId!, sendingProfilPic!));
                }
            }
            else if (task == RelationshipStateEnum.Accepted)
            {
                lock (Client.pendingLock)
                {
                    Client.pendingFriendRequestsList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                }
                lock (Client.friendsLock)
                {
                    Client.friendList.Add((sendingUsername!, sendingId!, sendingProfilPic!));
                }
            }
            else if (task == RelationshipStateEnum.Decline)
            {
                lock (Client.pendingLock)
                {
                    Client.pendingFriendRequestsList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                }
            }
            else if (task == RelationshipStateEnum.Delete)
            {
                lock (Client.friendsLock)
                {
                    Client.friendList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                var home = ClientUI.GetWindow<Home>();
                home?.PopulateFriendsList();
                home?.PopulatePendingFriendRequestsList();
            });
        }

        private static void WriteLoginDataIntoFile(string email, string password)
        {
            using (var isoStorage = IsolatedStorageFile.GetUserStoreForAssembly())
            using (var isoStream = new IsolatedStorageFileStream("UserLoginData.txt", FileMode.OpenOrCreate, isoStorage))
            using (var writer = new StreamWriter(isoStream))
            {
                writer.WriteLine(email);
                writer.WriteLine(password);
            }
        }
    }
}
