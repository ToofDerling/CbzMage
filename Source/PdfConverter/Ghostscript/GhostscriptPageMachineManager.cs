using Ghostscript.NET;
using Ghostscript.NET.Processor;
using Microsoft.Win32.SafeHandles;
using Microsoft.WinAny;
using PdfConverter.Helpers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPageMachineManager : IDisposable
    {
        private readonly ConcurrentBag<GhostscriptPageMachine> _stoppedMachines;

        private readonly ConcurrentDictionary<GhostscriptPageMachine, object> _runningMachines;

        private unsafe readonly byte* _libraryPtr;

        private readonly SafeMemoryMappedViewHandle _handle;

        public GhostscriptPageMachineManager(GhostscriptVersionInfo version)
        {
            var dllFile = new FileInfo(version.DllPath);

            using (var mappedFile = MemoryMappedFile.CreateFromFile(dllFile.FullName, FileMode.Open))
            //using (var view = mappedFile.CreateViewAccessor(0, dllFile.Length))
            using (var stream = mappedFile.CreateViewStream(0, dllFile.Length))
            {
                // Do the bitness check here where we have the memory mapped file - and fail fast
                // No need to check each time the library is loaded
                if (Environment.Is64BitProcess != Is64BitLibrary())
                {
                    throw new BadImageFormatException(version.DllPath);
                }

                _handle = stream.SafeMemoryMappedViewHandle;
                unsafe
                {
                    _handle.AcquirePointer(ref _libraryPtr);
                }

                bool Is64BitLibrary()
                {
                    var machine = NativeLibraryHelper.GetImageFileMachineType(stream);
                    return NativeLibraryHelper.Is64BitMachineValue(machine);
                };
            }

            _stoppedMachines = new ConcurrentBag<GhostscriptPageMachine>();
            _runningMachines = new ConcurrentDictionary<GhostscriptPageMachine, object>();
        }

        public GhostscriptPageMachine StartMachine()
        {
            if (!_stoppedMachines.TryTake(out var machine))
            {
                GhostscriptLibrary library;
                unsafe
                {
                    library = new GhostscriptLibrary(_libraryPtr);
                }

                //processorOwnsLibrary ensures that GhostscriptLibrary and in turn DynamicNativeLibrary are disposed
                var processor = new GhostscriptProcessor(library, processorOwnsLibrary: true);

                StatsCount.NewPageMachines++;
                machine = new GhostscriptPageMachine(processor);
            }
            else
            {
                StatsCount.CachedPageMachines++;
            }

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

                    unsafe
                    {
                        if (_libraryPtr != null)
                        {
                            _handle.ReleasePointer();
                        }
                    }
                    _handle.Dispose();

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
