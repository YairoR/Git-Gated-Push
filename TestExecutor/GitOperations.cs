using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TestExecutor
{
    /// <summary>
    /// Tool for executing Git operations (git push, git branch and etc..)
    /// </summary>
    public class GitOperations
    {
        /// <summary>
        /// In case the user didn't gave the git exe path, this is the default one.
        /// </summary>
        private const string GitDefaultPath = @"C:\Program Files (x86)\Git\cmd\git.exe";

        private ProcessStartInfo _gitProcessInfo;
        private ProcessStartInfo _cmdProcessInfo;
        private Process _process;

        /// <summary>
        /// Createa a new instance of <see cref="GitOperations"/> class.
        /// </summary>
        public GitOperations() : this(GitDefaultPath)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="GitOperations"/> class.
        /// </summary>
        /// <param name="gitPath">The git.exe path.</param>
        public GitOperations(string gitPath)
        {
            // Set git process
            _gitProcessInfo = new ProcessStartInfo(gitPath);
            _gitProcessInfo.UseShellExecute = false;
            _gitProcessInfo.RedirectStandardOutput = true;
            _gitProcessInfo.RedirectStandardInput = true;
            _gitProcessInfo.CreateNoWindow = true;

            // Set cmd process
            _cmdProcessInfo = new ProcessStartInfo("cmd");
            _cmdProcessInfo.UseShellExecute = false;
            _cmdProcessInfo.RedirectStandardInput = true;
            _cmdProcessInfo.RedirectStandardOutput = true;
            _cmdProcessInfo.RedirectStandardError = true;
            _cmdProcessInfo.CreateNoWindow = true;

            _process = new Process();
        }

        /// <summary>
        /// Invoke 'git push' command.
        /// </summary>
        /// <returns>The command's result.</returns>
        public string Push(string repositoryPath)
        {
            // Invoke git push command
            _gitProcessInfo.Arguments = "push";
            _process.StartInfo = _gitProcessInfo;

            _process.Start();

            return _process.StandardOutput.ReadToEnd();
        }

        /// <summary>
        /// Get the current branch that the user is currently in.
        /// </summary>
        /// <param name="repositoryPath">The repository path.</param>
        /// <returns>The current git branch.</returns>
        public string GetCurrentBranch(string repositoryPath)
        {
            // Get the specific branch that the user is in
            _gitProcessInfo.Arguments = "rev-parse --abbrev-ref HEAD";
            _gitProcessInfo.WorkingDirectory = repositoryPath;
            _process.StartInfo = _gitProcessInfo;

            _process.Start();

            if (!_process.StandardOutput.EndOfStream)
            {
                return _process.StandardOutput.ReadLine();
            }

            return null;
        }
    }
}
