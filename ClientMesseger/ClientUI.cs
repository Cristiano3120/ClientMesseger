using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientMesseger
{
    internal static class ClientUI
    {
        #region Methods for basic interaction with an window like closing.

        public static void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = FindVisualParent<Window>(button!);
            ChangeWindowState(window, WindowState.Minimized);
        }

        public static void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = FindVisualParent<Window>(button!);
            ChangeWindowState(window, WindowState.Maximized);
        }

        public static void BtnCloseShutdown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public static void BtnCloseCurrentWindow(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = FindVisualParent<Window>(button!);
            Application.Current.Dispatcher.Invoke(() =>
            {
                window?.Close();
            });
        }

        /// <summary>
        /// Moves the window.
        /// </summary>
        public static void Window_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is Window window)
            {
                if (window.WindowState == WindowState.Maximized)
                {

                    var mousePosition = Mouse.GetPosition((UIElement)sender);
                    window.WindowState = WindowState.Normal;
                    //Custom testet offsets
                    window.Left = mousePosition.X - 400;
                    window.Top = mousePosition.Y - 20;
                }
                window.DragMove();
                return;
            }
            else
            {
                window = Window.GetWindow(sender as DependencyObject);
                if (window.WindowState == WindowState.Maximized)
                {
                    
                    var mousePosition = Mouse.GetPosition((UIElement)sender);
                    window.WindowState = WindowState.Normal;
                    //Manuell getestete offsets
                    window.Left = mousePosition.X - 400;
                    window.Top = mousePosition.Y - 20;
                }
                window?.DragMove();
                return;
            }
        }

        #endregion

        public static T? GetWindow<T>() where T : Window
        {
            foreach (Window item in Application.Current.Windows)
            {
                if (item is T typedWindow)
                {
                    return typedWindow;
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
                        _ = DisplayError.LogAsync("Closing a window");
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
            _ = DisplayError.LogAsync("Error(ClientUI.ChangeWindowState(): var window was null)");
        }

        /// <typeparam name="T">The type of the wanted child</typeparam>
        /// <returns></returns>
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return default;

            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T wantedChild)
                {
                    return wantedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return default;
        }

        /// <summary>
        /// Searches for the parent of the enterd object.
        /// </summary>
        /// <typeparam name="T">The parent object.</typeparam>
        /// <param name="child">The child to search with.</param>
        /// <returns>If found the wanted obj.</returns>
        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
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

        public static double CheckIfCanSendRequest(Stopwatch stopwatch, float delay = 5)
        {
            if (stopwatch.IsRunning)
            {
                if (stopwatch.Elapsed.TotalSeconds >= delay)
                {
                    return 0;
                }
                return delay - stopwatch.Elapsed.TotalSeconds;
            }
            return -1;
        }
    }
}
