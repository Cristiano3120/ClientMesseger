using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientMesseger
{
    internal static class ClientUI
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
        public static void BtnCloseCurrentWindow(object sender, RoutedEventArgs e)
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

        public static Window? GetWindow(Type window)
        {
            foreach (Window item in Application.Current.Windows)
            {
                if (item.GetType() == window)
                {
                    Console.WriteLine("Returning window");
                    return item;
                }
            }
            return null;
        }

        public static void CloseAllWindowsExceptOne(Window window)
        {
            foreach (Window item in Application.Current.Windows)
            {
                if (item != window)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("Closing a window");
                        item.Close();
                    });
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
