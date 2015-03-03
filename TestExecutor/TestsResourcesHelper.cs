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
