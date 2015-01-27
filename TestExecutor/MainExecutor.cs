using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestExecutor
{
    public class MainExecutor
    {
        private GitGatedPushConfiguration _testsConfiguration;
        private readonly GitOperations _gitOperations;
        private SolutionBuilder _solutionBuilder;
        private ILogger _logger;
        private TestsHandler _testsHandler;

        private const string UniqueLogFolderFormat = "dd.MM.yyyy HH.mm.ss";

        /// <summary>
        /// Creates a new instance of the <see cref="MainExecutor"/> class
        /// </summary>
        public MainExecutor()
        {
            _testsConfiguration = new GitGatedPushConfiguration();
            _gitOperations = new GitOperations();
        }

        #region Main Method

        public bool Execute()
        {
            // Initialize trace listener
            var logsPath = InitializeTracer();

            try
            {
                // Set configuration
                if (!Initialize())
                {
                    return false;
                }

                // Validate if we need to run process on current branch
                if (!ValidateBranchName(Environment.CurrentDirectory))
                {
                    return false;
                }

                return InternalExecute(logsPath);
            }
            catch (Exception e)
            {
                Message.WriteError("Sorry, but we're having a problem...Please contact Yairip in order to investigate what the problem is.");
                Trace.TraceError("Problem has occured: {0}", e);
                return false;
            }
            finally
            {
                Trace.Flush();

                Message.WriteSuccess("You can find your logs here: {0}", logsPath);
            }
        }

        #endregion

        #region Private Methods

        private bool InternalExecute(string resourcesPath)
        {
            _solutionBuilder = new SolutionBuilder();

            // Get all solutions items we should work on
            var solutionItems = GetSolutionsItems(Environment.CurrentDirectory,
                                                  _testsConfiguration.ProcessAllSolutions,
                                                  _testsConfiguration.SolutionsItems).ToList();

            Trace.TraceInformation("Found {0} solution items: {1}",
                solutionItems.Count(),
                string.Join(", ",
                from item in solutionItems
                select new { item.SolutionPath, item.BuildSolution, item.RunTests }));

            // Check we have items to work on
            if (!solutionItems.Any())
            {
                Message.WriteError("Couldn't find any solution, proceeding in 'git push'.");
                return true;
            }

            // Start working on all the required solutions by building them and execute their tests if necessary 
            var result = true;
            foreach (var solutionItem in solutionItems)
            {
                result = result && HandleSolutionItem(solutionItem, resourcesPath);
            }

            return result;
        }

        private bool Initialize()
        {
            // Set configuration
            if (!SetConfiguration())
            {
                return false;
            }

            // Set objects with values from configuration
            _testsHandler = new TestsHandler(_testsConfiguration.MsTestPath);

            return true;
        }

        /// <summary>
        /// Set run configuration from user and default values.
        /// </summary>
        private bool SetConfiguration()
        {
            // Read configurations from file
            _testsConfiguration = GetTestsConfiguration(Environment.CurrentDirectory);

            // Check we got the configurations
            if (_testsConfiguration == null)
            {
                Message.WriteError("Can't find configuration file in {0}!", Environment.CurrentDirectory);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize the trace listener.
        /// </summary>
        /// <returns></returns>
        private string InitializeTracer()
        {
            var logsOutputPath = GetLogsFullPath();

            // Create log path
            var logPath = Path.Combine(logsOutputPath, "GitGatedPushLogs.log");

            Trace.Listeners.Add(new TextWriterTraceListener(logPath, "GitGatedPushTraceListener"));

            return logsOutputPath;
        }

        private string GetLogsFullPath()
        {
            var logsOutputPath = Path.GetTempPath();
            var folderName = DateTime.UtcNow.ToString(UniqueLogFolderFormat) + "-Logs-Output";
            var fullPath = Path.Combine(logsOutputPath, folderName);

            // Create physical path
            Directory.CreateDirectory(fullPath);

            return fullPath; 
        }

        /// <summary>
        /// Start handle solution item by building the solution and execute the unit tests.
        /// </summary>
        /// <param name="solutionItem">The solution item.</param>
        /// <returns>True if all steps passed, else False.</returns>
        private bool HandleSolutionItem(SolutionItem solutionItem, string resourcesPath)
        {
            Message.WriteInformation("Starting work on solution {0}", Path.GetFileNameWithoutExtension(solutionItem.SolutionPath));

            Trace.TraceInformation("Starting to process solution {0}", solutionItem.SolutionPath);

            var stopWatch = Stopwatch.StartNew();

            // Build solution
            var buildOutputPath = GetTempBuildFolderPath();
            Message.WriteInformation("Starting to build at: {0}", buildOutputPath);
            var solutionBuilt = BuildSolution(solutionItem, buildOutputPath, resourcesPath);

            // If the build failed, move to the next solution (don't continue to the tests)
            if (!solutionBuilt)
            {
                Trace.TraceInformation("Failed to build solution");

                // Clear build 
                TestsResourcesHelper.ClearResources(buildOutputPath);

                return false;
            }

            Trace.TraceInformation("Solution was built successfully");

            var testsResults = _testsHandler.FindAndExecuteTests(buildOutputPath).Result;

            Message.WriteInformation("Done working in {0} seconds.", stopWatch.Elapsed.Seconds);

            TestsResourcesHelper.ClearResources(buildOutputPath);

            return testsResults;
        }

        /// <summary>
        /// Find all solutions that need to be processed.
        /// If the user set in configuration to build all solutions, the return value will be all solutions in the repository path.
        /// Else, the return value will be all the solutions that the user specific, which exists.
        /// </summary>
        /// <param name="repositoryPath">The repository path.</param>
        /// <param name="findAllSolutions">Indicates if we should find all solutions</param>
        /// <param name="solutionItems">The solutions paths that the user indicates.</param>
        /// <returns></returns>
        public IEnumerable<SolutionItem> GetSolutionsItems(string repositoryPath, bool findAllSolutions, List<SolutionItem> solutionItems)
        {
            // Case we should find all solutions in repository path
            if (findAllSolutions)
            {
                Trace.TraceInformation("Configuration was set to find all solution in repository path {0}", repositoryPath);

                var solutionsPaths = Directory.GetFiles(repositoryPath, "*.sln", SearchOption.AllDirectories);

                Message.WriteInformation("Looking for all solutions in {0}. Found: {1}", repositoryPath, string.Join(",", solutionsPaths));

                Trace.TraceInformation("Found {0} solutions", solutionsPaths.Count());

                return solutionsPaths.Select(solutionPath =>
                    new SolutionItem
                    {
                        SolutionPath = Path.Combine(repositoryPath, solutionPath),
                        BuildSolution = true,
                        RunTests = true
                    });
            }
            else
            {
                Trace.TraceInformation("Configuration was set to search for specific solution items");

                var solutionsPaths = solutionItems.Select(s => Path.Combine(repositoryPath, s.SolutionPath));

                Message.WriteInformation("Looking for the following solutions: {0}", string.Join(", ", solutionsPaths));

                return solutionItems.Where(item => File.Exists(Path.Combine(repositoryPath, item.SolutionPath)));
            }
        }

        public GitGatedPushConfiguration GetTestsConfiguration(string rootPath)
        {
            // Search for 'TestsConfiguration.config' file
            var configFiles = Directory.GetFiles(rootPath, "GitGatedPushConfiguration.config", SearchOption.TopDirectoryOnly);

            // Check we found only 1 config file
            if (!configFiles.Any() || configFiles.Count() > 1)
            {
                Message.WriteError("There must be exactly one GitGatedPushConfiguration.config file.");
                return null;
            }

            return GitGatedPushConfiguration.LoadFromXmlFile(configFiles.First());
        }

        /// <summary>
        /// Build the given solution (only if the 'BuildSolution' property is true).
        /// </summary>
        /// <param name="solutionItem">The solution item.</param>
        /// <param name="buildOutputPath">The build output path.</param>
        /// <returns>True if the solution was built successfully, else False.</returns>
        private bool BuildSolution(SolutionItem solutionItem, string buildOutputPath, string resourcesPath)
        {
            // In case we don't need to build solution, skip this work item
            if (!solutionItem.BuildSolution)
            {
                return true;
            }

            Message.WritePrepare("Starting to build solution...");

            SpinAnimation.Start(75);

            // Build solution
            var buildSucceeded = _solutionBuilder.BuildSolution(solutionItem.SolutionPath,
                                                                buildOutputPath,
                                                                resourcesPath);

            SpinAnimation.Stop();

            if (!buildSucceeded)
            {
                Message.WriteError("Build for solution {0} failed!", solutionItem.SolutionPath);
            }
            else
            {
                Message.WriteSuccess("Build completed successfully!");
            }

            return buildSucceeded;
        }

        /// <summary>
        /// Validate that the git gated push should executed in the current branch or not.
        /// </summary>
        /// <param name="repositoryPath">The repository path.</param>
        /// <returns>True if validation succeeded (current branch is valid), else False.</returns>
        private bool ValidateBranchName(string repositoryPath)
        {
            // Get the current branch
            var branchName = _gitOperations.GetCurrentBranch(repositoryPath);

            Trace.TraceInformation("Current branch is: {0}", branchName);

            // In case we couldn't get the branch's name
            if (string.IsNullOrEmpty(branchName))
            {
                Message.WriteError("Couldn't able to get current branch's name. Please verify that your current dir is the repository's path.");
                Trace.TraceError("Failed to get current branch name!");

                return false;
            }

            // Check if we should continue only in 'develop' branch
            if (_testsConfiguration.OnDevelopOnly && !branchName.Equals("develop", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            } 

            Message.WriteInformation("Current branch is {0}. Start looking for solutions.", branchName);

            return true;
        }

        /// <summary>
        /// Get a temp output folder path for build solutions.
        /// </summary>
        /// <returns>The path for building solution.</returns>
        private string GetTempBuildFolderPath()
        {
            var tempFolder = Path.GetTempPath();
            var folderName = DateTime.UtcNow.ToString(UniqueLogFolderFormat) + "-Build-Output";
            var fullPath = Path.Combine(tempFolder, folderName);

            // Create physical path
            Directory.CreateDirectory(fullPath);

            return fullPath;
        }

        #endregion
    }
}
