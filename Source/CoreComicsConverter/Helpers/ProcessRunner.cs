using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreComicsConverter.Helpers
{
    public static class ProcessRunner
    {
        public static List<string> RunAndWaitForProcess(string path, string args, string workingDirectory, EventHandler<DataReceivedEventArgs> outputReceived)
        {
            return RunAndWaitForProcess(path, args, workingDirectory, outputReceived, ProcessPriorityClass.Idle);
        }

        public static List<string> RunAndWaitForProcess(string path, string args, string workingDirectory, EventHandler<DataReceivedEventArgs> outputReceived, ProcessPriorityClass priorityClass)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            if (outputReceived != null)
            {
                process.OutputDataReceived += (s, e) => outputReceived(s, e);
            }

            List<string> _errorLines = new List<string>();
            process.ErrorDataReceived += (s, e) => OnError(e.Data);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();

            if (outputReceived != null)
            {
                process.BeginOutputReadLine();
            }

            process.WaitForExit();

            return _errorLines;

            void OnError(string line)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    _errorLines.Add(line);
                }
            }
        }
    }
}
