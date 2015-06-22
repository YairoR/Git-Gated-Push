using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TestExecutor
{
    /// <summary>
    /// An helper class resources management.
    /// </summary>
    public static class TestsResourcesHelper
    {
        private const string UniqueLogFolderFormat = "dd.MM.yyyy HH.mm.ss";
        private static string _generatedBuildPath;
        private static string _generatedLogsPath;
        private static string _generatedTestsLogsPath;

        public static string GetBuildPath()
        {
            // In case we already generated the path
            if (_generatedBuildPath != null)
            {
                return _generatedBuildPath;
            }

            var tempFolder = Path.GetTempPath();
            var folderName = DateTime.UtcNow.ToString(UniqueLogFolderFormat) + "-Build-Output";
            var fullPath = Path.Combine(tempFolder, folderName);

            // Create physical path
            Directory.CreateDirectory(fullPath);

            _generatedBuildPath = fullPath;

            return fullPath;
        }

        public static string GetTempPathForTestRun()
        {
            // In case we already generated the path
            if (_generatedTestsLogsPath != null)
            {
                return Path.Combine(_generatedLogsPath, Path.GetTempFileName() + ".trx");
            }

            var fullPath = GetTestsLogsPath();

            return Path.Combine(fullPath, Path.GetTempFileName() + ".trx");
        }

        public static string GetTestsLogsPath()
        {
            // In case we already generated the path, just add new file name
            if (_generatedTestsLogsPath != null)
            {
                return _generatedTestsLogsPath;
            }

            var tempFolder = Path.GetTempPath();
            var folderName = DateTime.UtcNow.ToString(UniqueLogFolderFormat) + "-TestsRunLogs-Output";
            var fullPath = Path.Combine(tempFolder, folderName);

            // Create physical path
            Directory.CreateDirectory(fullPath);

            _generatedTestsLogsPath = fullPath;

            return fullPath;
        }

        public static string GetLogsPath()
        {
            // In case we already generated the path
            if (_generatedLogsPath != null)
            {
                return _generatedLogsPath;
            }

            var logsOutputPath = Path.GetTempPath();
            var folderName = DateTime.UtcNow.ToString(UniqueLogFolderFormat) + "-Logs-Output";
            var fullPath = Path.Combine(logsOutputPath, folderName);

            // Create physical path
            Directory.CreateDirectory(fullPath);

            _generatedLogsPath = fullPath;

            return fullPath;
        }

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
