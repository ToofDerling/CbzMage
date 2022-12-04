﻿using PdfConverter.Helpers;
using System.Collections.Concurrent;

namespace PdfConverter.ManagedBuffers
{
    public abstract class Cache<T> : IDisposable where T : class
    {
        private readonly ConcurrentStack<T> _cache;

        public Cache()
        {
            _cache = new ConcurrentStack<T>();
        }

        protected abstract T CreateNew();

        public virtual T Get()
        {
            var isCached = _cache.TryPop(out var buffer);

            if (!isCached)
            {
                buffer = CreateNew();
            }

#if DEBUG
            StatsCount.AddBuffer(isCached);
#endif

            return buffer;
        }

        public virtual void Release(T buffer)
        {
            _cache.Push(buffer);
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
                    var tempbuffers = _cache.ToArray();

                    for (int i = 0, sz = tempbuffers.Length; i < sz; i++)
                    {
                        if (tempbuffers[i] is IDisposable)
                        {
                            (tempbuffers[i] as IDisposable).Dispose();
                        }
                        tempbuffers[i] = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Cache()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
