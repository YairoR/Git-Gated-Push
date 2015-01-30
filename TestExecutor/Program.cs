using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace TestExecutor
{
    public class Program
    {
        private const int OperationSucceeded = 1;
        private const int OperationFailed = 0;

        public static int Main(string[] args)
        {
            var mainExecutor = new MainExecutor();
            var result = mainExecutor.Execute();

            // Return screen font color to gray (default)
            Console.ForegroundColor = ConsoleColor.Gray;

            return result ? OperationSucceeded : OperationFailed;
        }
    }
}
