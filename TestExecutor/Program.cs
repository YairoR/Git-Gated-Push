using System;
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
            try
            {
                var mainExecutor = new MainExecutor();
                var result = mainExecutor.ExecuteAsync();

                // Return screen font color to gray (default)
                Console.ForegroundColor = ConsoleColor.Gray;

                return result ? OperationSucceeded : OperationFailed;
            }
            catch (Exception ex)
            {
                Message.WriteError("Sorry, but we're having a problem...Please contact Yairip in order to investigate what the problem is.");
                File.WriteAllText("ExceptionDetails.txt", ex + "---------" + ex.InnerException, Encoding.UTF8);
                return 0;
            }
        }
    }
}
