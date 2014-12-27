﻿using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace TestExecutor
{
    public class Program
    {
        private static const int OperationSucceeded = 1;
        private static const int OperationFailed = 0;

        public static int Main(string[] args)
        {
            try
            {
                var mainExecutor = new MainExecutor();
                var result = mainExecutor.ExecuteAsync();

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