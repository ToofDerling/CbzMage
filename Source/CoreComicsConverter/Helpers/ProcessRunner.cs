using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreComicsConverter.Helpers
{
    public class ProcessRunner
    {
        private List<string> _errorLines = new List<string>();

        public void RunAndWaitForProcess(string path, string switches, Action<string> outputLineReader, string workingDirectory, ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal)
        {
            using var process = GetProcess(path, switches, outputLineReader, workingDirectory);

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

        private Process GetProcess(string path, string args, Action<string> outputLineReader, string workingDirectoy)
        {
            var process = new Process();

            process.OutputDataReceived += (s, e) => outputLineReader(e.Data);
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
