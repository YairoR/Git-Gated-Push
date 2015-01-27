using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TestExecutor
{
    /// <summary>
    /// The MSTest component executer - execute a test container using the MSTest component.
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// The MSTest component path.
        /// </summary>
        private string _mstestPath;

        /// <summary>
        /// Initialize a new instance of <see cref="TestRunner"/> class.
        /// </summary>
        /// <param name="MsTestPath">The MSTest component full path.</param>
        public TestRunner(string MsTestPath)
        {
            _mstestPath = MsTestPath;
        }

        /// <summary>
        /// By a given tests container full path, execute the tests using MSTest.
        /// </summary>
        /// <param name="testContainer">The tests container path.</param>
        /// <returns>MSTest output result.</returns>
        public Task<string> RunTestContainerAsync(string testContainer)
        {
            using (var process = new Process()
            {
                StartInfo = new ProcessStartInfo(_mstestPath)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            })
            {
                process.StartInfo.Arguments = "/testcontainer:" + testContainer + " /resultsfile:" + Path.GetTempFileName() + ".trx";
                process.Start();

                return process.StandardOutput.ReadToEndAsync();
            }
        }
    }
}
