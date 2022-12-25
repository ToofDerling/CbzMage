using System.Diagnostics;

namespace CbzMage.Shared.Helpers
{
    public class ProcessRunner : IDisposable
    {
        private readonly List<string> _errorLines = new();

        private readonly Process _process;

        private readonly ProcessPriorityClass _priorityClass;

        public ProcessRunner(string path, string args = "", string workingDirectory = "",
            ProcessPriorityClass processPriority = ProcessPriorityClass.Normal, EventHandler<DataReceivedEventArgs>? outputReceived = null)
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };

            if (outputReceived != null)
            {
                _process.OutputDataReceived += (s, e) => outputReceived(s, e);
            }

            _process.ErrorDataReceived += (s, e) => OnError(e.Data!);

            _priorityClass = processPriority;
        }

        public void Run()
        {
            _process.Start();

            if (_priorityClass != ProcessPriorityClass.Normal && !_process.HasExited)
            {
                try
                {
                    _process.PriorityClass = _priorityClass;
                }
                catch
                {
                    //This can fail if process has already exited, so ignore any error
                }
            }

            _process.BeginErrorReadLine();
        }

        /// <summary>
        /// Get the underlying output stream. Do not touch Process.StandardOutput after doing this.
        /// </summary>
        /// <returns></returns>
        public Stream GetOutputStream()
        {
            var stream = _process.StandardOutput.BaseStream;
            return stream;
        }

        public int RunAndWaitForExit()
        {
            Run();

            _process.WaitForExit();

            return _process.ExitCode;
        }

        private void OnError(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                _errorLines.Add(line);
            }
        }

        public List<string> GetStandardErrorLines()
        {
            return _errorLines;
        }

        public int WaitForExitCode()
        {
            if (!_process.HasExited)
            {
                _process.WaitForExit();
            }
            return _process.ExitCode;
        }

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _process.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProcessRunner()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
