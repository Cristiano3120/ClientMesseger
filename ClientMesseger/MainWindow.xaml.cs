using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientMesseger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IWindowExtras 
    {
        public MainWindow()
        {
            InitializeComponent();
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            _ = StartClient();
        }

        public static async Task StartClient()
        {
            await Client.Start();
        }

        public void CloseWindow()
        {
            btnMinimize.Click -= ClientUI.BtnMinimize_Click;
            btnMaximize.Click -= ClientUI.BtnMaximize_Click;
            btnClose.Click -= ClientUI.BtnCloseShutdown_Click;
            MouseLeftButtonDown -= ClientUI.Window_MouseLeftButtonDown;
            Close();
        }
    }
}