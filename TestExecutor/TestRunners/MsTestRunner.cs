using System.Diagnostics;
using System.Threading.Tasks;

namespace TestExecutor.TestRunners
{
    /// <summary>
    /// The MSTest component executer - execute a test container using the MSTest component.
    /// </summary>
    internal class MsTestRunner
    {
        /// <summary>
        /// The MSTest component path.
        /// </summary>
        private readonly string _mstestPath;

        /// <summary>
        /// Initialize a new instance of <see cref="MsTestRunner"/> class.
        /// </summary>
        /// <param name="msTestPath">The MSTest component full path.</param>
        public MsTestRunner(string msTestPath)
        {
            _mstestPath = msTestPath;
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
                    RedirectStandardOutput = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            })
            {
                process.StartInfo.Arguments = string.Format("/testcontainer:\"{0}\" /resultsfile:\"{1}\"", testContainer, TestsResourcesHelper.GetTempPathForTestRun());
                process.Start();

                return process.StandardOutput.ReadToEndAsync();
            }
        }
    }
}
