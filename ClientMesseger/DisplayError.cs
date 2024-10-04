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
        private static readonly Queue<(string, string)> _loggingList = new();

        public static void Initialize()
        {
            if (File.Exists(_loggingFile))
            {
                File.WriteAllText(_loggingFile, "");
            }
        }

        public static void DisplayBasicErrorInfos(Exception ex, string className, string methodName)
        {
            _ = LogAsync($"Error({className}.{methodName}): {ex.Message}");
        }

        public static void ObjectDisposedException(ObjectDisposedException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            _ = LogAsync($"Error: The object {ex.ObjectName} was disposed");
        }

        public static void SocketException(SocketException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            _ = LogAsync($"Error(ErrorCode, SocketErrorCode): {ex.ErrorCode}, {ex.SocketErrorCode}");
        }

        public static void ArgumentNullException(ArgumentNullException ex, string className, string methodName)
        {
            DisplayBasicErrorInfos(ex, className, methodName);
            _ = LogAsync($"Error(Var that was null): {ex.ParamName}");
        }

        public static async Task LogAsync<T>(T log) where T : IConvertible
        {
            try
            {
                Console.WriteLine(log);
                _loggingList.Enqueue((log.ToString()!, $"[{DateTime.UtcNow.ToString("HH:mm:ss")}]"));
                foreach (var (content, timestamp) in _loggingList)
                {
                    using (var writer = new StreamWriter(_loggingFile, true))
                    {
                        await writer.WriteLineAsync($"{timestamp} {content}");
                    }
                }
            }
            catch (Exception)
            {
                _loggingList.Enqueue((log.ToString()!, $"[{DateTime.UtcNow.ToString("HH:mm:ss")}]"));
            }
        }
    }
}
