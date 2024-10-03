using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientMesseger
{
    /// <summary>
    /// The logic to communicate with the Server.
    /// </summary>
    internal static class Client
    {
#pragma warning disable CS8618
        private static TcpClient _client;
        public static BitmapImage ProfilPicture { get; private set; }
        public static string? Username { get; private set; }
        public static int Id { get; private set; }
        public static readonly List<(string, int, string)> friendList = new();
        public static readonly List<(string, int, string)> pendingFriendRequestsList = new();
        public static readonly List<(string, int, string)> blockedList = new();
        public static readonly object friendsLock = new();
        public static readonly object pendingLock = new();
        public static readonly object blockedLock = new();

        public static async Task Start()
        {
            AllocConsole();
            DisplayError.Initialize();
            _client = new TcpClient();
            var cancellationToken = new CancellationTokenSource().Token;
            var ip = IPAddress.Parse("192.168.178.74");
            var port = 50000;
        Connect:
            try
            {
                _ = DisplayError.Log("Trying to connect to server");
                await _client.ConnectAsync(ip, port, cancellationToken);
                _ = DisplayError.Log("Connection to server succesful");
                Security.Initialize();
                _ = Task.Run(() => { _ = ListenForMessages(); });
                var loadingWindow = ClientUI.GetWindow<MainWindow>();
                if (loadingWindow == null) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var login = new Login();
                    login.Show();
                    loadingWindow?.Close();
                });
            }
            catch (SocketException ex)
            {
                DisplayError.SocketException(ex, "Client", "Start()");
                await Task.Delay(3000);
                goto Connect;
            }
        }

        /// <summary>
        /// Restarts the Application if the connection to the Server is lost.
        /// </summary>
        private static void Restart()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var loadingScreen = new MainWindow();
                loadingScreen.Show();
                ClientUI.CloseAllWindowsExceptOne(loadingScreen);
            });
        }

        private static void TryToAutoLogin()
        {
            using var isoStorage = IsolatedStorageFile.GetUserStoreForAssembly();
            var fileName = "UserLoginData.txt";
            if (isoStorage.FileExists(fileName))
            {
                _ = DisplayError.Log("Trying to auto login");
                using var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStorage);
                using var reader = new StreamReader(isoStream);
                var email = reader.ReadLine();
                var password = reader.ReadLine();
                if (email != null && password != null)
                {
                    var payload = new
                    {
                        code = 8,
                        email,
                        password
                    };
                    var jsonString = JsonSerializer.Serialize(payload);
                    _ = SendPayloadAsync(jsonString);
                }
            }
            else
            {
                _ = DisplayError.Log("File for auto login couldnt be found.");
            }
        }

        private static async Task ListenForMessages()
        {
            var buffer = new byte[32768];
            while (_client.Connected)
            {
                try
                {
                    var bytesRead = await _client.Client.ReceiveAsync(buffer);
                    var tempBuffer = new byte[bytesRead];
                    Array.Copy(buffer, tempBuffer, bytesRead);
                    var root = Security.DecryptMessage(tempBuffer) ?? throw new Exception("Root was null");
                    var code = root.GetProperty("code").GetByte();
                    _ = DisplayError.Log($"Received code {code}");
                    _ = DisplayError.Log($"Received: {root}");
                    switch (code)
                    {
                        case 0: //Receiving RSA key
                            Security.SaveRSAKey(root);
                            break;
                        case 4: //Response trying to make an acc
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                //OnReceivedCode4?.Invoke(root);
                                var createAcc = ClientUI.GetWindow<CreateAccount>();
                                createAcc?.ResponseCode4(root);
                            });
                            break;
                        case 7: //Feedback Creating acc
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
                                    var username = root.GetProperty(nameof(Username)).GetString();
                                    var profilPic = root.GetProperty("profilPic").GetString();
                                    Username = username;
                                    ProfilPicture = GetBitmapImageFromBase64String(profilPic!)!;
                                    if (ProfilPicture == null)
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
                            break;
                        case 9: //Reiceving answer to the login request
                            var result = root.GetProperty("result");
                            if (result.ValueKind == JsonValueKind.Null)
                            {
                                _ = DisplayError.Log("Server couldnt connect to the database.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var login = ClientUI.GetWindow<Login>();
                                    login?.CallErrorBox("The request can´t be send right now. Try again later.");
                                });
                                break;
                            }
                            var canLogin = root.GetProperty("result").GetBoolean();
                            if (canLogin)
                            {
                                var email = root.GetProperty("email").GetString();
                                var password = root.GetProperty("password").GetString();
                                Username = root.GetProperty("username").GetString();
                                Id = root.GetProperty("id").GetInt32();
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
                                ProfilPicture = bitmap;

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var home = new Home();
                                    home = ClientUI.GetWindow<Home>()!;
                                    home.OnProfilPicChanged(ProfilPicture);
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
                            break;
                        case 11: //Answer to the sent friendRequest
                            result = root.GetProperty("result");
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
                            break;
                        case 12: //Reiceving Friend request
                            var username = root.GetProperty("usernameSender").GetString();
                            var senderId = root.GetProperty("senderId").GetInt32();
                            var profilPic = root.GetProperty("profilPic").GetString();
                            _ = DisplayError.Log($"{username} added you!");
                            lock (pendingLock)
                            {
                                pendingFriendRequestsList.Add((username!, senderId, profilPic!));
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var home = ClientUI.GetWindow<Home>();
                                home?.PopulatePendingFriendRequestsList();
                            });
                            break;
                        case 13: //Getting friends and pending requests
                            Console.WriteLine(root.ToString());
                            if (root.TryGetProperty("friends", out var friendsElement) && friendsElement.ValueKind == JsonValueKind.Array)
                            {
                                var friendsList = JsonSerializer.Deserialize<List<Friend>>(friendsElement.GetRawText());
                                foreach (var friend in friendsList!)
                                {
                                    if (friend.Status == RelationshipStateEnum.Pending)
                                    {
                                        _ = DisplayError.Log(RelationshipStateEnum.Pending);
                                        lock (pendingLock)
                                        {
                                            pendingFriendRequestsList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
                                        }
                                    }
                                    else if (friend.Status == RelationshipStateEnum.Accepted)
                                    {
                                        _ = DisplayError.Log(RelationshipStateEnum.Accepted);
                                        lock (friendsLock)
                                        {
                                            friendList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
                                        }
                                    }
                                    else if (friend.Status == RelationshipStateEnum.Blocked)
                                    {
                                        _ = DisplayError.Log("BLOCKED");
                                        lock (blockedLock)
                                        {
                                            blockedList.Add((friend.Username, friend.FriendId, friend.ProfilPic));
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
                            break;
                        case 16: //Server is ready to receive messages
                            //TryToAutoLogin();
                            break;
                        case 17:
                            var sendingUsername = root.GetProperty("username").GetString();
                            var sendingId = root.GetProperty("userId").GetInt32();
                            var sendingProfilPic = root.GetProperty("profilPic").GetString();
                            var task = (RelationshipStateEnum)root.GetProperty("taskByte").GetByte();
                            if (task == RelationshipStateEnum.Blocked)
                            {
                                lock (friendsLock)
                                {
                                    friendList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                                lock (blockedLock)
                                {
                                    blockedList.Add((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                            }
                            else if (task == RelationshipStateEnum.Accepted)
                            {
                                lock (pendingLock)
                                {
                                    pendingFriendRequestsList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                                lock (friendsLock)
                                {
                                    friendList.Add((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                            }
                            else if (task == RelationshipStateEnum.Decline)
                            {
                                lock (pendingLock)
                                {
                                    pendingFriendRequestsList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                            }
                            else if (task == RelationshipStateEnum.Delete)
                            {
                                lock (friendsLock)
                                {
                                    friendList.Remove((sendingUsername!, sendingId!, sendingProfilPic!));
                                }
                            }
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var home = ClientUI.GetWindow<Home>();
                                home?.PopulateFriendsList();
                                home?.PopulatePendingFriendRequestsList();
                            });
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DisplayError.DisplayBasicErrorInfos(ex, "Client", "ListenForMessages()");
                }
            }
            _ = DisplayError.Log("Lost connection to the Server");
            Restart();
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

        public static async Task SendPayloadAsync(string payload, EncryptionMode encryption = EncryptionMode.AES)
        {
            try
            {
                _ = DisplayError.Log($"Sending: {payload}");
                _ = DisplayError.Log($"Trying to send {encryption} encrypted data");
                var buffer = payload != null ? Encoding.UTF8.GetBytes(payload) : throw new ArgumentNullException(nameof(payload));
                switch (encryption)
                {
                    case EncryptionMode.AES:
                        buffer = Security.EncryptDataAes(buffer);
                        break;
                    case EncryptionMode.RSA:
                        buffer = Security.EncryptRSAData(buffer);
                        break;
                }
                await _client.Client.SendAsync(buffer);
            }
            catch (SocketException ex)
            {
                DisplayError.SocketException(ex, "Client", "SendPayloadAsync()");
            }
            catch (ObjectDisposedException ex)
            {
                DisplayError.ObjectDisposedException(ex, "Client", "SendPayloadAsync()");
            }
            catch (ArgumentNullException)
            {
                _ = DisplayError.Log($"Error(Client.SendPayloadAsync(): Payload was null)");
            }
            catch (Exception ex)
            {
                _ = DisplayError.Log($"Error(Client.SendPayloadAsync()): {ex.Message}");
            }
        }

        public static BitmapImage? GetBitmapImageFromBase64String(string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String)) return null;
                var imageBytes = Convert.FromBase64String(base64String);

                using (var ms = new MemoryStream(imageBytes))
                {
                    var bitmapImage = new BitmapImage();
                    ms.Position = 0;

                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                DisplayError.DisplayBasicErrorInfos(ex, "Client", "GetBitmapImageFromBase64String");
                return null;
            }
        }

        public static void SetProfilPicture(BitmapImage image)
        {
            ProfilPicture = image;
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}

