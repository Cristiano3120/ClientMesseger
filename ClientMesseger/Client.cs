using System.CodeDom;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace ClientMesseger
{
    /// <summary>
    /// The logic to communicate with the Server.
    /// </summary>
    internal static class Client
    {
        #pragma warning disable CS8618
        private static TcpClient _client;
        internal delegate void MessageReceivedEventHandler(JsonElement root);
        internal static event MessageReceivedEventHandler OnReceivedCode4;

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
                Console.WriteLine("Trying to connect to server");
                await _client.ConnectAsync(ip, port, cancellationToken);
                Console.WriteLine("Connection to server succesful");
                AccessFriendlistFile(FileModeEnum.Create);
                Security.Initialize();
                _ = Task.Run(() => { _ = ListenForMessages(); });
                var loadingWindow = ClientUI.GetWindow(typeof(MainWindow));
                if (loadingWindow == null) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var login = new Login();
                    login.Show();
                    loadingWindow.Close();
                });
                TryToAutoLogin();
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

        public static List<string>? AccessFriendlistFile(FileModeEnum fileMode, string personToAdd = "")
        {
            var filename = "Friendlist.txt";
            try
            {
                using var isoStorage = IsolatedStorageFile.GetUserStoreForAssembly();

                if (!isoStorage.FileExists(filename) && fileMode != FileModeEnum.Create)
                {
                    Restart();
                    throw new FileNotFoundException("Friendlist file wasn´t found!");
                }

                switch (fileMode)
                {
                    case FileModeEnum.Create:
                        if (!isoStorage.FileExists(filename))
                        {
                            isoStorage.CreateFile(filename);
                        }
                    return null;    

                    case FileModeEnum.Read:
                        using (var file = isoStorage.OpenFile(filename, FileMode.Open))
                        using (var reader = new StreamReader(file))
                        {
                            var friends = new List<string>();
                            string? friend;
                            while ((friend = reader.ReadLine()) != null)
                            {
                                friends.Add(friend);
                            }
                            return friends;
                        }

                    case FileModeEnum.Write:
                        using (var file = isoStorage.OpenFile(filename, FileMode.Append))
                        using (var writer = new StreamWriter(file))
                        {
                            writer.WriteLine(personToAdd);
                            return null;
                        }             
                }
                return null;
            }
            catch (Exception ex)
            {
                DisplayError.DisplayBasicErrorInfos(ex, "Client", "AccessFriendlist");
                return null;
            }
        }

        private static void TryToAutoLogin()
        {
            using var isoStorage = IsolatedStorageFile.GetUserStoreForAssembly();
            var fileName = "UserLoginData.txt";
            if (isoStorage.FileExists(fileName))
            {
                Console.WriteLine("Trying to auto login");
                using IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStorage);
                using StreamReader reader = new StreamReader(isoStream);
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
                Console.WriteLine("File for auto login couldnt be found.");
            }
        }

        private static async Task ListenForMessages()
        {
            var buffer = new byte[8092];
            while (_client.Connected)
            {
                try
                {
                    int bytesRead = await _client.Client.ReceiveAsync(buffer);
                    var tempBuffer = new byte[bytesRead];
                    Array.Copy(buffer, tempBuffer, bytesRead);
                    var root = Security.DecryptMessage(tempBuffer) ?? throw new Exception("Root was null");
                    var code = root.GetProperty("code").GetByte();
                    Console.WriteLine($"Received code {code}");
                    switch (code)
                    {
                        case 0: //Receiving RSA key
                            Security.SaveRSAKey(root);
                            break;
                        case 4: //Response trying to make an acc
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                OnReceivedCode4?.Invoke(root);
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
                                Console.WriteLine("Server couldnt connect to the database.");
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var loginWindow = ClientUI.GetWindow(typeof(Login)) as Login;
                                    if (loginWindow != null)
                                    {
                                        _ = loginWindow.CallErrorBox("The request can´t be send right now. Try again later.");
                                    }
                                });
                                break;
                            }
                            var canLogin = root.GetProperty("result").GetBoolean();
                            switch (canLogin)
                            {
                                case true:
                                    var email = root.GetProperty("email").GetString();
                                    var password = root.GetProperty("password").GetString();
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        var home = new Home();
                                        home.Show();
                                        using (var isoStorage = IsolatedStorageFile.GetUserStoreForAssembly())
                                        using (var isoStream = new IsolatedStorageFileStream("UserLoginData.txt", FileMode.OpenOrCreate, isoStorage))
                                        using (var writer = new StreamWriter(isoStream))
                                        {
                                            writer.WriteLine(email);
                                            writer.WriteLine(password);
                                        }
                                        ClientUI.CloseAllWindowsExceptOne(home);
                                    });
                                    break;
                                default:
                                    _ = Application.Current.Dispatcher.BeginInvoke(() =>
                                    {
                                        var login = ClientUI.GetWindow(typeof(Login)) as Login;
                                        var error = canLogin == false ? "Email or password was wrong" : "An error accoured while processing ur request. Try again!";
                                        _ = login!.CallErrorBox(error);
                                    });
                                    break;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error(ListenForMessages(): {ex.Message})");
                }
            }
            Console.WriteLine("Lost connection to the Server");
            Restart();
        }

        public static async Task SendPayloadAsync(string payload, EncryptionMode encryption = EncryptionMode.AES)
        {
            try
            {
                Console.WriteLine($"Trying to send {encryption} encrypted data");
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
                Console.WriteLine($"Error(Client.SendPayloadAsync(): Payload was null)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error(Client.SendPayloadAsync()): {ex.Message}");
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
