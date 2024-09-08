using System.IO;
using System.Net.Sockets;

namespace ClientMesseger
{
    /// <summary>
    /// Offers methods for error output and logging
    /// </summary>
    internal static class DisplayError
    {
        private const string _loggingFile = @"C:\Users\Crist\Desktop\ClientLog.txt";

        public static void Initialize()
        {
            if (File.Exists(_loggingFile))
            {
                File.WriteAllText(_loggingFile, "");
            }
        }

        public static void DisplayBasicErrorInfos(Exception ex, string className, string methodName)
        {
            var error = $"Error({className}.{methodName}): {ex.Message}";
            Console.WriteLine(error);
            Log(error);
        }

        public static void ObjectDisposedException(ObjectDisposedException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            var error = $"Error: The object {ex.ObjectName} was disposed";
            Console.WriteLine($"Error: The object {ex.ObjectName} was disposed");
            Log(error);
        }

        public static void SocketException(SocketException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            var error = $"Error(ErrorCode, SocketErrorCode): {ex.ErrorCode}, {ex.SocketErrorCode}";
            Console.WriteLine(error);
            Log(error);
        }

        private static void Log(string content)
        {
            if (File.Exists(_loggingFile))
            {
                using (var writer = new StreamWriter(_loggingFile, false))
                {
                    writer.WriteLine(content);
                }
            }
            else
            {
                Console.WriteLine("Couldn´t write the error into the logging File because it doesn´t exist.");
            }
        }
    }
}
