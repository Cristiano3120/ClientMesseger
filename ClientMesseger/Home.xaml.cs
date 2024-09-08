using System.Windows;

namespace ClientMesseger
{
    /// <summary>
    /// Interaktionslogik für Home.xaml
    /// </summary>
    public sealed partial class Home : Window
    {
        public Home()
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
        }
    }
}