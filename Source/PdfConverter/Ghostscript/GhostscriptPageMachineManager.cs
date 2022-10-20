using CbzMage.Shared.Helpers;
using Ghostscript.NET;
using Ghostscript.NET.Processor;
using PdfConverter.Helpers;
using System.Collections.Concurrent;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPageMachineManager : IDisposable
    {
        private readonly ConcurrentBag<GhostscriptPageMachine> _stoppedMachines;

        private readonly ConcurrentDictionary<GhostscriptPageMachine, object> _runningMachines;

        private readonly GhostscriptLibrary _library;

        public static GhostscriptVersionInfo GetGhostscriptVersion()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Console.WriteLine("Sorry, at the moment pdf to cbz conversion only works on Windows");

                return null;
            }

            var gsVersion = GhostscriptVersionInfo.GetInstalledVersions()
                .OrderByDescending(gs => gs.Version)
                .FirstOrDefault();

            if (gsVersion == null || gsVersion.Version.Major < Settings.GsMinVersion)
            {
                ProgressReporter.Error("CbzMage requires Ghostscript version 10+ is installed");
                if (gsVersion != null)
                {
                    ProgressReporter.Info($"Found Ghostscript version {gsVersion.Version}");
                }
                return null;

            }
            return gsVersion;
        }

        public GhostscriptPageMachineManager(GhostscriptVersionInfo gsVersion)
        {
            _library = new GhostscriptLibrary(gsVersion);

            _stoppedMachines = new ConcurrentBag<GhostscriptPageMachine>();
            _runningMachines = new ConcurrentDictionary<GhostscriptPageMachine, object>();
        }

        public GhostscriptPageMachine StartMachine()
        {
            var isCached = _stoppedMachines.TryTake(out var machine);
            
            if (!isCached)
            {
                var processor = new GhostscriptProcessor(_library);
                machine = new GhostscriptPageMachine(processor);
            }

#if DEBUG
            StatsCount.AddPageMachine(isCached); 
#endif
            
            if (!_runningMachines.TryAdd(machine, null))
            {
                throw new InvalidOperationException("machine alredy running");
            }

            return machine;
        }

        public void StopMachine(GhostscriptPageMachine machine)
        {
            if (!_runningMachines.TryRemove(machine, out _))
            {
                throw new InvalidOperationException("machine not running");
            }

            _stoppedMachines.Add(machine);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    foreach (var stopped in _stoppedMachines)
                    {
                        stopped.Dispose();
                    }

                    //_runningMachines should be empty at this point
                    try
                    {
                        foreach (var running in _runningMachines)
                        {
                            running.Key.Dispose();
                        }
                    }
                    catch
                    {
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GhostscriptPageMachineFactory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
