using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace ClientMesseger
{
    /// <summary>
    /// The logic behind the Verification screen (Verification.xaml)
    /// </summary>
    public sealed partial class Verification : Window
    {
        private readonly User _user;
        private readonly CreateAccount _window;
        private readonly long _verificationCode;
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Subscribes to the important events and initializes some vars.
        /// </summary>
        /// <param name="window">The create account window in case the user wants to go back to it.</param>
        /// <param name="user">Information about the user. 
        /// This is needed so it can be put into the Database later if the enterd verification code is correct.</param>
        /// <param name="verificationCode"> The generated verification code.</param>
        public Verification(CreateAccount window, User user, long verificationCode)
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            _window = window;
            _user = user;
            _verificationCode = verificationCode;
            _stopwatch = new Stopwatch();
        }

        #region Buttons_Click
        /// <summary>
        /// The click logic for the hyperlink.
        /// It closes this window and opens up an Create account window.
        /// </summary>
        private async void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            _window.Show();
            await Task.Delay(3000);
            Close();
        }

        /// <summary>
        /// When the this button (verify button) is clicked it checks if the enterd code
        /// matches with the send code.
        /// Else it will display an error
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double timeLeft = ClientUI.CheckIfCanSendRequest(_stopwatch);
            if (timeLeft > 0)
            {
                InfoBox.Text = $"You need to wait {timeLeft} more seconds till you can send another request.";
                return;
            }
            else if (timeLeft <= 0)
            {
                _stopwatch.Restart();
            }
            if (_verificationCode.ToString() == VerifyBox.Text)
            {
                var payload = new
                {
                    code = 6,
                    _user.Email,
                    _user.Username,
                    _user.Password,
                    _user.FirstName,
                    _user.LastName,
                    _user.Day,
                    _user.Month,
                    _user.Year,
                };
                var jsonString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(jsonString);

            }
            else
            {
                InfoBox.Text = "The enterd code was wrong!";
            }
        }

        #endregion

        #region TextBox Input check

        /// <summary>
        /// Only allows the input if the IsTextAllowed method returns true.
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        /// <summary>
        /// If the input matches with the allowed pattern this will return true.
        /// </summary>
        /// <param name="text">The input</param>
        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        #endregion
    }
}
