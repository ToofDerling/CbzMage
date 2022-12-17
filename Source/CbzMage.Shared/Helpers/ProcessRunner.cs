using System.Diagnostics;

namespace CbzMage.Shared.Helpers
{
    public class ProcessRunner
    {
        private readonly List<string> _errorLines = new();

        public int RunAndWaitForProcess(string path, params string[] args)
        {
            var parameters = string.Join(' ', args);
            return RunAndWaitForProcess(path, parameters);
        }

        public int RunAndWaitForProcess(string path, string args = "", string workingDirectory = "", 
            ProcessPriorityClass priorityClass = ProcessPriorityClass.Normal, EventHandler<DataReceivedEventArgs>? outputReceived = null)
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

            process.ErrorDataReceived += (s, e) => OnError(e.Data);

            process.Start();

            process.PriorityClass = priorityClass;

            process.BeginErrorReadLine();

            if (outputReceived != null)
            {
                process.BeginOutputReadLine();
            }

            process.WaitForExit();

            return process.ExitCode;

            void OnError(string line)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    _errorLines.Add(line);
                }
            }
        }

        public List<string> GetStandardErrorLines()
        {
            return _errorLines;
        }
    }
}
