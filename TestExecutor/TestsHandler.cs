using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TestExecutor
{
    internal class TestsHandler
    {
        private TestRunner _testRunner;
        private readonly XmlSerializer _xmlSer = new XmlSerializer(typeof(TestRunType));
        private readonly List<string> _failingTests = new List<string>();
        private readonly TestsContainerFinder _testContainerFinder = new TestsContainerFinder();

        public TestsHandler(string MsTestPath)
        {
            _testRunner = new TestRunner(MsTestPath);
        }


        /// <summary>
        /// Run the unit tests in the given test container.
        /// </summary>
        /// <param name="buildPath">The test container's name.</param>
        /// <returns>The tests results.</returns>
        public async Task<bool> FindAndExecuteTests(string buildPath)
        {
            Message.WriteInformation("Searching for tests containers");

            var testsContainers = LookForTestsContainer(buildPath);

            // Run all tests async
            var testResultsInfo = await Task.WhenAll(testsContainers.Select(_testRunner.RunTestContainerAsync));

            // In each test result, find the trx file and convert it to TestRunType object
            var testsResults = testResultsInfo.Select(GetTestResult).Where(t => t != null).ToList();

            // Calculate the number of passed and failed tests
            var passedTests = CountTestsResults(testsResults, TestOutcome.Passed).ToList();
            var failureTests = CountTestsResults(testsResults, TestOutcome.Failed).ToList();

            if (failureTests.Count() != 0)
            {
                // Show failed tests
                Message.WriteError("There are failing tests:");
                foreach (var test in failureTests)
                {
                    Message.WriteInformation(test);
                }
            }

            ShowResults(passedTests.Count, failureTests.Count);

            return failureTests.Count == 0;
        }

        /// <summary>
        /// Get <see cref="TestRunType"/> from given trx file.
        /// </summary>
        /// <param name="testResult">The test result file (trx).</param>
        /// <returns>The test run type.</returns>
        private TestRunType GetTestResult(string testResult)
        {
            try
            {
                var resultFileStartLocation = testResult.IndexOf("Results file:", StringComparison.Ordinal);
                var trxPathEndLocation = testResult.IndexOf("\r\n", resultFileStartLocation, StringComparison.Ordinal);

                var trxPathStartLocation = resultFileStartLocation + "Results file:".Length;
                var trxPathLength = trxPathEndLocation - trxPathStartLocation;

                // Get the trx file path
                var trxPath = testResult.Substring(trxPathStartLocation, trxPathLength);

                var trxFile = new FileInfo(trxPath);

                using (var trxFileReader = new StreamReader(trxFile.FullName))
                {
                    return (TestRunType)_xmlSer.Deserialize(trxFileReader);
                }
            }
            catch (Exception)
            {
                Message.WriteError(@"There is a test container that failed to run.. This might cause because of tests which are not unit tests.");
                return null;
            }
        }

        /// <summary>
        /// Count the number of tests results by the required outcome (Passed, Failed and etc..).
        /// </summary>
        /// <param name="tests">The tests trxs.</param>
        /// <param name="outcome">The required out come.</param>
        /// <returns>The count of tests results by the given outcome.</returns>
        private IEnumerable<string> CountTestsResults(IEnumerable<TestRunType> tests, TestOutcome outcome)
        {
            var results = new List<string>();

            foreach (var test in tests)
            {
                var items = test.Items[6] as ResultsType;
                results.AddRange(from p in items.Items
                                 where (p as UnitTestResultType).outcome.Equals(outcome.ToString())
                                 select (p as UnitTestResultType).testName);
            }

            return results;
        }

        private IEnumerable<string> LookForTestsContainer(string buildPath)
        {
            var watch = Stopwatch.StartNew();

            Message.WriteInformation("Starting looking for tests...");

            var testContainers = _testContainerFinder.Execute(buildPath).ToList();

            Message.WriteSuccess("Done in : {0}", watch.Elapsed.TotalSeconds);

            return testContainers;
        }

        /// <summary>
        /// Show the tests results to client.
        /// </summary>
        /// <param name="numberOfPassedTests">The number of passed tests.</param>
        /// <param name="numberOfFailureTests">The number of failed tests.</param>
        /// <returns>Whether we should proceed with the operation or stop.</returns>
        private bool ShowResults(int numberOfPassedTests, int numberOfFailureTests)
        {
            Console.WriteLine("Number of passed tests: {0}.", numberOfPassedTests);
            Console.WriteLine("Number of failed tests: {0}.", numberOfFailureTests);

            if (numberOfFailureTests != 0)
            {
                return false;
            }

            return true;
        }
    }
}
