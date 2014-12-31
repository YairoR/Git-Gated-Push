using System;
using System.IO;
using System.Linq;

namespace TestExecutor
{
    public static class TestsResourcesHelper
    {
        public const string LogPath = @"C:\LogResultsTempFolder\";
        public const string BuildPath = @"c:\BuildOutputTempFolder\";
        public const string GitPushExtentionLogs = @"C:\GitPushExtentionLogs\";
        public const string BuildLogPath = GitPushExtentionLogs + @"SolutionBuilderLogs.txt";

        /// <summary>
        /// Create all needed resources for running the tests.
        /// </summary>
        public static void CreateResource()
        {
            ClearResources();
            Directory.CreateDirectory(LogPath);
            Directory.CreateDirectory(BuildPath);
            Directory.CreateDirectory(GitPushExtentionLogs);

            using (var f = File.Create(BuildLogPath))
            {
                // Do nothing       
            }
        }

        /// <summary>
        /// Clear all used resources for running the tests.
        /// </summary>
        public static void ClearResources()
        {
            // Check if logs folder is exists, if so - delete it
            if (Directory.Exists(LogPath))
            {
                var directory = new DirectoryInfo(LogPath);

                directory.EnumerateFiles()
                    .ToList().ForEach(f =>
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Do Nothing
                        }
                    });

                directory.EnumerateDirectories()
                    .ToList().ForEach(d => d.Delete(true));
               
                // Delete the log's path
                Directory.Delete(LogPath);
            }

            // Check if build folder is exists, if so - delete it
            if (Directory.Exists(BuildPath))
            {
                var directory = new DirectoryInfo(BuildPath);

                directory.EnumerateFiles()
                    .ToList().ForEach(f =>
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Do Nothing
                        }
                    });

                directory.EnumerateDirectories().ToList().ForEach(d => d.Delete(true));

                // Delete directory
                Directory.Delete(BuildPath);
            }
        }
    }
}
