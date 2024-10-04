using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClientMesseger
{
    /// <summary>
    /// The Logic to communicate with the Server.
    /// </summary>
    internal static class Client
    {
        #pragma warning disable CS8618
        private static TcpClient _client;
        public static BitmapImage ProfilPicture { get; set; }
        public static string? Username { get; set; }
        public static int Id { get; set; }
        public static readonly List<Friend> relationshipState = new();
        public static readonly object relationshipStateLock = new();

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
                _ = DisplayError.LogAsync("Trying to connect to server");
                await _client.ConnectAsync(ip, port, cancellationToken);
                _ = DisplayError.LogAsync("Connection to server succesful");
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
                _ = DisplayError.LogAsync("Trying to auto Login");
                using var isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStorage);
                using var reader = new StreamReader(isoStream);
                var email = reader.ReadLine();
                var password = reader.ReadLine();

                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
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
                _ = DisplayError.LogAsync("File for auto Login couldnt be found.");
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
                    _ = DisplayError.LogAsync($"Received code {code}");
                    _ = DisplayError.LogAsync($"Received: {root}");

                    switch (code)
                    {
                        case 0: //Receiving RSA key
                            Security.SaveRSAKey(root);
                            break;
                        case 4: //Response trying to make an acc
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                var createAcc = ClientUI.GetWindow<CreateAccount>();
                                createAcc?.ResponseCode4(root);
                            });
                            break;
                        case 7: //Feedback Creating acc
                            HandleServerMessages.FeedbackAccountCreation(root);
                            break;
                        case 9: //Reiceving answer to the Login request
                            HandleServerMessages.FeedbackLogin(root);
                            break;
                        case 11: //Answer to the sent friendRequest
                            HandleServerMessages.FeedbackSentFriendrequest(root);
                            break;
                        case 12: //Reiceving Friend request
                            HandleServerMessages.ReceiveFriendrequest(root);
                            break;
                        case 13: //Getting friends and pending requests
                            HandleServerMessages.ReceiveRelationshipStates(root);
                            break;
                        case 16: //Server is ready to receive messages
                            TryToAutoLogin();
                            break;
                        case 17: //Updating a relationship(blocked, deleted etc..)
                            HandleServerMessages.UpdateRelationship(root);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DisplayError.DisplayBasicErrorInfos(ex, "Client", "ListenForMessages()");
                }
            }
            _ = DisplayError.LogAsync("Lost connection to the Server");
            Restart();
        }

        public static async Task SendPayloadAsync(string payload, EncryptionMode encryption = EncryptionMode.AES)
        {
            try
            {
                _ = DisplayError.LogAsync($"Sending: {payload}");
                _ = DisplayError.LogAsync($"Trying to send {encryption} encrypted data");
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
                _ = DisplayError.LogAsync($"Error(Client.SendPayloadAsync(): Payload was null)");
            }
            catch (Exception ex)
            {
                _ = DisplayError.LogAsync($"Error(Client.SendPayloadAsync()): {ex.Message}");
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

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}

