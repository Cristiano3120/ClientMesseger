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
            Log($"Error({className}.{methodName}): {ex.Message}");
        }

        public static void ObjectDisposedException(ObjectDisposedException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            Log($"Error: The object {ex.ObjectName} was disposed");
        }

        public static void SocketException(SocketException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            Log($"Error(ErrorCode, SocketErrorCode): {ex.ErrorCode}, {ex.SocketErrorCode}");
        }

        public static void ArgumentNullException(ArgumentNullException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            Log($"Error(Var that was null): {ex.ParamName}");
        }

        public static void Log(string error)
        {
            Console.WriteLine(error);
            using (var writer = new StreamWriter(_loggingFile, true))
            {
                writer.WriteLine($"[{DateTime.UtcNow.ToString("HH:mm:ss")}] {error}");
            }
        }
    }
}
