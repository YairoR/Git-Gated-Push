using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TestExecutor
{
    public class MainExecutor
    {
        private GitGatedPushConfiguration _testsConfiguration = new GitGatedPushConfiguration();
        private readonly GitOperations _gitOperations = new GitOperations();
        private readonly SolutionBuilder _solutionBuilder = new SolutionBuilder();
        private TestsHandler _testsHandler;

        #region Main Method

        public bool ExecuteAsync()
        {
            try
            {
                // Read configurations from file
                _testsConfiguration = GetTestsConfiguration(Environment.CurrentDirectory);

                // Check we got the configurations
                if (_testsConfiguration == null)
                {
                    Message.WriteError("Can't find configuration file in {0}!", Environment.CurrentDirectory);
                    return false;
                }

                // Set objects with values from configuration
                _testsHandler = new TestsHandler(_testsConfiguration.MsTestPath);

                // Create resources
                TestsResourcesHelper.CreateResource();

                // Get the current branch
                if (!ValidateBranchName(Environment.CurrentDirectory))
                {
                    return false;
                }

                // Get all solutions items we should work on
                var solutionItems = GetSolutionsItems(Environment.CurrentDirectory,
                                                 _testsConfiguration.ProcessAllSolutions,
                                                 _testsConfiguration.SolutionsItems).ToList();

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
                    result = result && HandleSolutionItem(solutionItem);
                }

                return result;
            }
            finally
            {
                TestsResourcesHelper.ClearResources();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Start handle solution item by building the solution and execute the unit tests.
        /// </summary>
        /// <param name="solutionItem">The solution item.</param>
        /// <returns>True if all steps passed, else False.</returns>
        private bool HandleSolutionItem(SolutionItem solutionItem)
        {
            Message.WriteInformation("Starting work on solution {0}", Path.GetFileNameWithoutExtension(solutionItem.SolutionRelativePath));

            var stopWatch = Stopwatch.StartNew();

            // Build solution
            var solutionBuilt = BuildSolution(solutionItem);

            // If the build failed, move to the next solution (don't continue to the tests)
            if (!solutionBuilt)
            {
                return false;
            }

            var testsResults = _testsHandler.ExecuteTests(TestsResourcesHelper.BuildPath).Result;

            Message.WriteInformation("Done working in {0} seconds.", stopWatch.Elapsed.Seconds);

            TestsResourcesHelper.ClearResources();

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
                var solutionsPaths = Directory.GetFiles(repositoryPath, "*.sln", SearchOption.AllDirectories);
                
                Message.WriteInformation("Looking for all solutions in {0}. Found: {1}", repositoryPath, string.Join(",", solutionsPaths));
                
                return solutionsPaths.Select(solutionPath =>
                    new SolutionItem
                    {
                        SolutionRelativePath = Path.Combine(repositoryPath, solutionPath),
                        BuildSolution = true,
                        RunTests = true
                    });
            }
            else
            {
                Message.WriteInformation("Looking for the following solutions: {0}", string.Join(", ", solutionItems.Select(s => s.SolutionRelativePath)));

                return solutionItems.Where(item => File.Exists(item.SolutionRelativePath));
            }
        }

        public GitGatedPushConfiguration GetTestsConfiguration(string rootPath)
        {
            // Search for 'TestsConfiguration.config' file
            var configFiles = Directory.GetFiles(rootPath, "TestsConfiguration.config", SearchOption.TopDirectoryOnly);

            // Check we found only 1 config file
            if (!configFiles.Any() && configFiles.Count() > 1)
            {
                Message.WriteError("There must be exactly one TestsConfiguration.config file.");
                return null;
            }

            return GitGatedPushConfiguration.LoadFromXmlFile(configFiles.First());
        }

        /// <summary>
        /// Build the given solution (only if the 'BuildSolution' property is true).
        /// </summary>
        /// <param name="solutionItem">The solution item.</param>
        /// <returns>True if the solution was built successfully, else False.</returns>
        private bool BuildSolution(SolutionItem solutionItem)
        {
            // In case we don't need to build solution, skip this work item
            if (!solutionItem.BuildSolution)
            {
                return true;
            }

            Message.WritePrepare("Starting to build solution...");

            SpinAnimation.Start(75);

            // Build solution
            var buildSucceeded = _solutionBuilder.BuildSolution(solutionItem.SolutionRelativePath);

            SpinAnimation.Stop();

            if (!buildSucceeded)
            {
                Message.WriteError("Build for solution {0} failed!", solutionItem.SolutionRelativePath);
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

            // In case we couldn't get the branch's name
            if (string.IsNullOrEmpty(branchName))
            {
                Message.WriteError("Couldn't able to get current branch's name. Please verify that your current dir is the repository's path.");
                return false;
            }

            // Check if we should continue only in 'develop' branch
            if (_testsConfiguration.OnDevelopOnly && !branchName.Equals("develop", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            Message.WriteInformation("Current branch is {0}. Start looking for solutions.", branchName);

            return true;
        }

        #endregion
    }
}
