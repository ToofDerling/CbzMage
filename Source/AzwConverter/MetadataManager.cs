using MobiMetadata;
using System.Collections.Concurrent;

namespace AzwConverter
{
    public class MetadataManager
    {
        public static MobiMetadata.MobiMetadata ConfigureFullMetadata()
        {
            // Want the image records of course, but not any properties
            var pdbHeader = new PDBHead(skipProperties: true);

            // Nothing from this one
            var palmDocHeader = new PalmDOCHead(skipProperties: true);

            // Want the exth header,
            // fullname,
            // idx of first image record,
            // idx of last content record 
            var mobiHeader = new MobiHead();

            // Want the publisher,
            // the cover record index offset,
            // thumbnail record index offset 
            var exthHeader = MobiHeaderFactory.CreateReadAll<EXTHHead>();
            
            MobiHeaderFactory.ConfigureRead(exthHeader, exthHeader.PublisherAttr,
                exthHeader.CoverOffsetAttr, exthHeader.ThumbOffsetAttr);

            return new MobiMetadata.MobiMetadata(pdbHeader, palmDocHeader, mobiHeader, exthHeader, throwIfNoExthHeader: true);
        }

        private class CacheItem
        { 
            public MobiMetadata.MobiMetadata Metadata { get; set; }

            public IDisposable[] Disposables { get; set; }
        }

        private static readonly ConcurrentDictionary<string, CacheItem> cache = new();

        public static void CacheMetadata(string bookId, MobiMetadata.MobiMetadata metadata, 
            params IDisposable[] disposables)
        {
            var item = new CacheItem { Metadata = metadata, Disposables = disposables };

            if (!cache.TryAdd(bookId, item))
            {
                throw new Exception($"Metadata for book {bookId} is already cached");
            }
        }

        public static MobiMetadata.MobiMetadata? GetCachedMetadata(string bookId)
        {
            return cache.TryGetValue(bookId, out var item) ? item.Metadata : default;
        }

        public static void DisposeCachedMetadata(string bookId)
        {
            if (cache.TryRemove(bookId, out var item))
            {
                Dispose(item.Disposables);
                item.Metadata = null;
            }
        }

        public static void Dispose(params IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public static void ThrowIfCacheNotEmpty()
        {
            if (!cache.IsEmpty)
            {
                throw new InvalidDataException("Boo hoo");
            }
        }
    }
}
