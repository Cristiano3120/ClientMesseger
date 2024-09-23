using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientMesseger
{
    /// <summary>
    /// The logic behind the Account creation screen (CreateAccount.xaml)
    /// </summary>
    public sealed partial class CreateAccount : Window
    {
#pragma warning disable CS8618
        private readonly Window _loginWindow;
        private readonly Stopwatch _stopwatch;
        private User _user;

        #region Constructors

        /// <summary>
        /// The constructor that is called when a error happend while creating an account
        /// </summary>
        /// <param name="error">The error message</param>
        public CreateAccount(string error)
        {
            InitializeComponent();
            _stopwatch = new Stopwatch();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            Client.OnReceivedCode4 += ResponseCode4;
            _loginWindow = new Login();
            _ = CallErrorBox(error);
        }

        /// <summary>
        /// The constructor that is called when the user wants to create an account.
        /// Gets called from Login.xaml.cs.
        /// </summary>
        /// <param name="window">The login screen gets saved so it can be opend or closed later.</param>
        public CreateAccount(Window window)
        {
            InitializeComponent();
            _stopwatch = new Stopwatch();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            Client.OnReceivedCode4 += ResponseCode4;
            _loginWindow = window;
        }

        #endregion

        #region Buttons_Click

        /// <summary>
        /// This button (SignUpButton) Checks the user inputs for errors.
        /// If none is found it sends these infos about the user to the Server.
        /// </summary>
        public void SignUpButton_Click(object sender, RoutedEventArgs args)
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
            if (CheckEmailSyntax() && CheckForWhiteSpaces())
            {
                _user = new User()
                {
                    Email = Email.Text,
                    Password = Password.Text,
                    Username = Username.Text,
                    FirstName = FirstName.Text,
                    LastName = LastName.Text,
                    Day = int.Parse(Day.Text),
                    Month = int.Parse(Month.Text),
                    Year = int.Parse(Year.Text),
                };

                var payload = new
                {
                    code = 2,
                    _user.Email,
                    _user.Username,
                };
                var payloadString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(payloadString);
            }
        }

        /// <summary>
        /// When pressed it opens up a Login window and closes this.
        /// </summary>
        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loginWindow == null)
            {
                var loginWindow = new Login();
                loginWindow.Show();
            }
            else
            {
                _loginWindow.Show();
            }
            Close();
        }

        #endregion

        #region Checking user input

        public bool CheckForWhiteSpaces()
        {
            if (!string.IsNullOrWhiteSpace(Username.Text))
            {
                foreach (var c in Username.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The username can't contain any whitespaces");
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(FirstName.Text))
            {
                foreach (var c in FirstName.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The first name can't contain any whitespaces");
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(LastName.Text))
            {
                foreach (var c in LastName.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The last name can't contain any whitespaces");
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Day.Text))
            {
                foreach (var c in Day.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The day can't contain any whitespaces");
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Month.Text))
            {
                foreach (var c in Month.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The month can't contain any whitespaces");
                        return false;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Year.Text))
            {
                foreach (var c in Year.Text)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        _ = CallErrorBox("The year can't contain any whitespaces");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if the user is allowed to enter more chars and if the input char is an allowed char.
        /// </summary>
        private void TextBox_PreviewTextInputDay(object sender, TextCompositionEventArgs e)
        {
            if (IsTextAllowed(e.Text))
            {
                var textBox = sender as TextBox;
                var currentText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

                if (int.TryParse(currentText, out int day))
                {
                    e.Handled = day < 1 || day > 31;
                }
                else
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Checks if the user is allowed to enter more chars and if the input char is an allowed char.
        /// </summary>
        private void TextBox_PreviewTextInputMonth(object sender, TextCompositionEventArgs e)
        {
            if (IsTextAllowed(e.Text))
            {
                var textBox = sender as TextBox;
                var currentText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

                if (int.TryParse(currentText, out int month))
                {
                    e.Handled = month < 1 || month > 12;
                }
                else
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Checks if the user is allowed to enter more chars and if the input char is an allowed char.
        /// </summary>
        private void TextBox_PreviewTextInputYear(object sender, TextCompositionEventArgs e)
        {
            if (IsTextAllowed(e.Text))
            {
                var textBox = sender as TextBox;
                var currentText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

                if (int.TryParse(currentText, out int year))
                {
                    e.Handled = year < 1 || year > DateTime.Now.Year;
                }
                else
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Checks if the input matches the regex pattern.
        /// </summary>
        /// <param name="text">The input.</param>
        /// <returns>Returns true if the text matches the pattern.</returns>
        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^[0-9]+$");
        }

        #endregion

        #region On Textbox text changed

        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount(Username, usernameCharCount);
        }

        private void Password_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount(Password, passwordCharCount);
        }

        private void FirstName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount(FirstName, firstNameCharCount);
        }

        private void LastName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharCount(LastName, lastNameCharCount);
        }

        /// <summary>
        /// Updates the number of chars the user has left to type into the box.
        /// </summary>
        /// <param name="textBox">The textbox that needs to be updated.</param>
        /// <param name="charCountBlock">The textblock to dislpay how many chars can still be enterd.</param>
        private void UpdateCharCount(TextBox textBox, TextBlock charCountBlock)
        {
            int maxLength = textBox.MaxLength;
            int remainingChars = maxLength - textBox.Text.Length;
            charCountBlock.Text = $"{remainingChars} chars remaining";
        }

        #endregion

        /// <summary>
        /// Checks if the Server allowed the enterd username and email.
        /// If yes the an verification window is opend and the server gets contacted to send an email and save the user data for later.
        /// If not an error message will be shown.
        /// </summary>
        /// <param name="root">The data as Json</param>
        public void ResponseCode4(JsonElement root)
        {
            var status = root.GetProperty("status").GetString();
            if (status == "None")
            {
                DisplayError.Log("Acc can be created!");
                var verificationCode = new Random().Next(100000, 999999);
                var payload = new
                {
                    code = 5,
                    _user.Email,
                    _user.Username,
                    _user.Password,
                    _user.FirstName,
                    _user.LastName,
                    _user.Day,
                    _user.Month,
                    _user.Year,
                    VerifyCode = verificationCode,
                };
                var jsonString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(jsonString);
                var loginWindow = _loginWindow;
                loginWindow!.Close();
                DisplayError.Log($"Code: {verificationCode}");
                var verification = new Verification(this, _user, verificationCode);
                verification.Show();
                Close();
            }
            else
            {
                DisplayError.Log($"The {status} is already being used. Pls enter another one!");
                _ = CallErrorBox($"The {status} is already being used. Pls enter another one!");
            }
        }

        /// <summary>
        /// Checks the syntax of the email.
        /// </summary>
        /// <returns>Returns true when the email hs the right syntax.</returns>
        public bool CheckEmailSyntax()
        {
            if (Email.Text.Contains('@'))
            {
                return true;
            }
            _ = CallErrorBox("Pls enter a valid email");
            return false;
        }

        /// <summary>
        /// Reveals a Error box to the user which will display the error message for 5 seconds
        /// </summary>
        /// <param name="errorText">The error</param>
        public async Task CallErrorBox(string errorText)
        {
            ErrorTextblock.Text = errorText;
            ErrorGrid.Visibility = Visibility.Visible;
            await Task.Delay(5000);
            ErrorTextblock.Text = string.Empty;
            ErrorGrid.Visibility = Visibility.Hidden;
        }
    }
}
