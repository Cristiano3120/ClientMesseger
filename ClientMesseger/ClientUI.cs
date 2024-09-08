using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientMesseger
{
    internal sealed class ClientUI
    {
        #region Methods for basic interaction with an window like closing.

        /// <summary>
        /// Minimizes the window.
        /// </summary>
        public static void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            Window? window = FindVisualParent<Window>(button!);
            ChangeWindowState(window, WindowState.Minimized);
        }

        /// <summary>
        /// Maximizes the window.
        /// </summary>
        public static void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            Window? window = FindVisualParent<Window>(button!);
            ChangeWindowState(window, WindowState.Maximized);
        }

        /// <summary>
        /// Completly closes the application.
        /// </summary>
        public static void BtnCloseShutdown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            Window? window = FindVisualParent<Window>(button!);
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Only closes the current window.
        /// </summary>
        private static void BtnCloseCurrentWindow(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            Window? window = FindVisualParent<Window>(button!);
            Application.Current.Dispatcher.Invoke(() =>
            {
                window?.Close();
                return;
            });
        }

        /// <summary>
        /// Moves the window.
        /// </summary>
        public static void Window_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                window.DragMove();
                return;
            }
            Console.WriteLine("Error(ClientUI.Window_MouseLeftButtonDown()): window was null");
        }

        #endregion

        /// <summary>
        /// Gets an open window by searching for it in the current opned windows list.
        /// </summary>
        /// <param name="window">The type of window it should search for</param>
        /// <returns>If found it returns the window</returns>
        public static IWindowExtras? GetWindow(Type window)
        {
            foreach (Window item in Application.Current.Windows)
            {
                if (item.GetType() == window)
                {
                    Console.WriteLine("Returning window");
                    return item as IWindowExtras;
                }
            }
            return null;
        }

        public static void CloseAllWindowsExceptOne(Type window)
        {
            foreach (Window item in Application.Current.Windows)
            {
                if (item.GetType() != window)
                {
                    if (item is IWindowExtras windowToClose)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Console.WriteLine("Closing a window");
                            windowToClose.CloseWindow();
                        });
                    }
                }
            }
        }

        public static void ChangeWindowState(Window? window, WindowState windowState)
        {
            if (window != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (windowState == WindowState.Maximized && window.WindowState == windowState)
                    {
                        window.WindowState = WindowState.Normal;
                        return;
                    }
                    window.WindowState = windowState;
                });
                return;
            }
            Console.WriteLine("Error(ClientUI.ChangeWindowState(): var window was null)");
        }

        /// <summary>
        /// Searches for the parent of the enterd object.
        /// </summary>
        /// <typeparam name="T">The parent object.</typeparam>
        /// <param name="child">The child to search with.</param>
        /// <returns>If found the wanted obj.</returns>
        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            while (parentObject != null)
            {
                if (parentObject is T parent)
                {
                    return parent;
                }
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }

        /// <summary>
        /// Checks if the user can send a request to the Server or if he has to wait.
        /// </summary>
        /// <param name="stopwatch">The stopwatch which indicates when the last request was sent.</param>
        /// <returns>Returns <c>-1</c> or <c>0</c> if the user can send a request.
        /// Else the user has to wait the amount of seconds that this returns.</returns>
        public static double CheckIfCanSendRequest(Stopwatch stopwatch)
        {
            if (stopwatch.IsRunning)
            {
                if (stopwatch.Elapsed.TotalSeconds >= 5)
                {
                    return 0;
                }
                return 5 - stopwatch.Elapsed.TotalSeconds;
            }
            return -1;
        }
    }
}
