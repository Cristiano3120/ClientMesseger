using System.Diagnostics;
using System.Text.Json;
using System.Windows;

namespace ClientMesseger
{
    /// <summary>
    /// The logic behind the Login screen (Login.xaml)
    /// </summary>
    public sealed partial class Login : Window, IWindowExtras
    {
        private readonly Stopwatch _stopwatch;

        public Login()
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            _stopwatch = new Stopwatch();
        }

        #region Button_Clicks

        public void LoginBtn_Click(object sender, RoutedEventArgs args)
        {
            double timeLeft = ClientUI.CheckIfCanSendRequest(_stopwatch);
            if (timeLeft > 0)
            {
                _ = CallErrorBox($"Wait {timeLeft:F1} more seconds until you can send a request again");
                return;
            }
            else if (timeLeft <= 0)
            {
                _stopwatch.Restart();
            }
            Console.WriteLine("Sending request to login");
            var email = Email.Text;
            var password = Password.Text;
            var payload = new
            {
                code = 8,
                email,
                password,
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString);
        }

        public void CreateAccHyperlink_Click(object sender, RoutedEventArgs args)
        {
            var createAccWindow = new CreateAccount(this);
            createAccWindow.Show();
            Hide();
        }

        #endregion

        public void CloseWindow()
        {
            btnMinimize.Click -= ClientUI.BtnMinimize_Click;
            btnMaximize.Click -= ClientUI.BtnMaximize_Click;
            btnClose.Click -= ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown -= ClientUI.Window_MouseLeftButtonDown;
            Close();
        }

        /// <summary>
        /// Reveals a Error box to the user which will display the error message for 5 seconds
        /// </summary>
        /// <param name="errorText">The error</param>
        public async Task CallErrorBox(string errorText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorTextBlock.Text = errorText;
                ErrorGrid.Visibility = Visibility.Visible;
            });

            await Task.Delay(5000);

            Application.Current.Dispatcher.Invoke(() =>
            {
                ErrorTextBlock.Text = string.Empty;
                ErrorGrid.Visibility = Visibility.Hidden;
            });
        }
    }
}
