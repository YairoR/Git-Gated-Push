using System;

namespace TestExecutor
{
    /// <summary>
    /// Writes messages to console with different types of colors.
    /// </summary>
    public static class Message
    {
        public static void WritePrepare(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteInformation(string message, params object[] values)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message, values);
        }

        public static void WriteError(string message, params object[] values)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, values);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteSuccess(string message, params object[] values)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message, values);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
