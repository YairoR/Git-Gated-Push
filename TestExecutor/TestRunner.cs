using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TestExecutor
{
    public class TestRunner
    {
        private string _mstestPath;

        public TestRunner(string MsTestPath)
        {
            _mstestPath = MsTestPath;
        }

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
                process.StartInfo.Arguments = "/testcontainer:" + testContainer + " /resultsfile:" + TestsResourcesHelper.LogPath + Guid.NewGuid() + ".trx";
                process.Start();

                return process.StandardOutput.ReadToEndAsync();
            }
        }
    }
}
