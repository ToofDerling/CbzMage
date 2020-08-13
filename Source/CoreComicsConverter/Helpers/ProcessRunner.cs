using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreComicsConverter.Helpers
{
    public class ProcessRunner
    {
        private List<string> _errorLines = new List<string>();

        public void RunAndWaitForProcess(string path, string switches, string workingDirectory, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
            using var process = GetProcess(path, switches, workingDirectory);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
        }

        private void OnError(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                _errorLines.Add(line);
            }
        }

        public List<string> GetErrorLines()
        {
            return _errorLines;
        }

        public event EventHandler<DataReceivedEventArgs> OutputReceived;

        private Process GetProcess(string path, string args, string workingDirectoy)
        {
            var process = new Process();

            process.OutputDataReceived += (s, e) => OutputReceived?.Invoke(this, e);
            process.ErrorDataReceived += (s, e) => OnError(e.Data);

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;

            process.StartInfo.WorkingDirectory = workingDirectoy;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            return process;
        }
    }
}
