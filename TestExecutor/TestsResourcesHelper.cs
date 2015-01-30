using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TestExecutor
{
    public static class TestsResourcesHelper
    {
        public const string LogPath = @"C:\LogResultsTempFolder\";
        public const string BuildPath = @"C:\BuildOutputTempFolder\";
        public const string GitPushExtentionLogs = @"C:\GitPushExtentionLogs\";
        public const string BuildLogPath = GitPushExtentionLogs + @"SolutionBuilderLogs.txt";

        /// <summary>
        /// Clear the given path from files and then delete the directory itself.
        /// </summary>
        /// <param name="resourcePath">The path to delete.</param>
        public static void ClearResources(string resourcePath)
        {
            Trace.TraceInformation("Starting to clear resource {0}", resourcePath);
            var allFilesDeleted = true;

            if (Directory.Exists(resourcePath))
            {
                var directory = new DirectoryInfo(resourcePath);

                directory.EnumerateFiles()
                    .ToList().ForEach(f =>
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch (UnauthorizedAccessException)
                        {
                            allFilesDeleted = false;
                            Trace.TraceError("Unable to delete file {0}", f);
                        }
                    });

                directory.EnumerateDirectories().ToList().ForEach(d => d.Delete(true));

                // If we succeeded to delete all files, delete the directory itself.
                // If not, it's ok to leave it as is because it's already in 'temp' folder
                if (allFilesDeleted)
                {
                    Directory.Delete(resourcePath);
                }
            }
        }
    }
}
