using System;
using System.Diagnostics;

namespace GameFramework.Editor.Build
{
    /// <summary>
    /// Commit/branch/dirty state captured by running the local <c>git</c> executable read-only
    /// (<c>rev-parse</c>, <c>status --porcelain</c>). It never reads remotes, config, or credentials.
    /// If Git is missing or the project is not a repository, <see cref="Available"/> is false and the
    /// fields hold <c>"unknown"</c>; the build continues and the fallback is reported.
    /// </summary>
    public sealed class GitInfo
    {
        public const string Unknown = "unknown";
        private const int TimeoutMilliseconds = 10000;

        public bool Available { get; private set; }
        public string Commit { get; private set; } = Unknown;
        public string Branch { get; private set; } = Unknown;
        public bool IsDirty { get; private set; }

        /// <summary>Why capture failed, for the report. Null when <see cref="Available"/>.</summary>
        public string FailureReason { get; private set; }

        public static GitInfo Unavailable(string reason) => new GitInfo { FailureReason = reason };

        public static GitInfo Capture(string workingDirectory)
        {
            try
            {
                if (!TryRun(workingDirectory, "rev-parse HEAD", out string commit) || commit.Length == 0)
                {
                    return Unavailable("Not a Git repository, or Git is not installed / not on PATH.");
                }

                TryRun(workingDirectory, "rev-parse --abbrev-ref HEAD", out string branch);
                bool statusOk = TryRun(workingDirectory, "status --porcelain", out string status);

                return new GitInfo
                {
                    Available = true,
                    Commit = commit,
                    Branch = string.IsNullOrEmpty(branch) ? Unknown : branch,
                    // If status itself failed, reporting "clean" could let a dirty release through.
                    IsDirty = !statusOk || status.Length > 0
                };
            }
            catch (Exception exception)
            {
                // Starting the process can throw (no git executable). Reported via the fallback.
                return Unavailable($"Git could not be run: {exception.GetType().Name}.");
            }
        }

        private static bool TryRun(string workingDirectory, string arguments, out string output)
        {
            var startInfo = new ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    output = string.Empty;
                    return false;
                }

                string text = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();

                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                        // Already exited between the timeout and Kill - nothing to clean up.
                    }

                    output = string.Empty;
                    return false;
                }

                output = text.Trim();
                return process.ExitCode == 0;
            }
        }
    }
}
