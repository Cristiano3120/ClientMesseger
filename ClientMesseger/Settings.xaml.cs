using System.Windows;
using System.Windows.Input;

namespace ClientMesseger
{
    public sealed partial class Settings : Window
    {
        private readonly Window _homeWindow;
        public Settings(Window window)
        {
            InitializeComponent();
            _homeWindow = window;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            KeyDown += (object sender, KeyEventArgs args) =>
            {
                if (args.Key == Key.Escape)
                {
                    _homeWindow.Show();
                    Close();
                }
            };
        }
    }
}
