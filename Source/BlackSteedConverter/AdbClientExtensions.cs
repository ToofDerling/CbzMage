using AdvancedSharpAdbClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackSteedConverter
{
    internal static class AdbClientExtensions
    {
        public static FileStatistics StatDevicePath(this IAdbClient client, DeviceData device, string path)
        {
            using ISyncService service = Factories.SyncServiceFactory(client, device);
            return service.Stat(path);
        }

        public static IEnumerable<FileStatistics> List(this IAdbClient client, DeviceData device, string path)
        {
            using ISyncService service = Factories.SyncServiceFactory(client, device);
            return service.GetDirectoryListing(path);
        }

        public static void Pull(this IAdbClient client, DeviceData device, string path, Stream stream,
            EventHandler<SyncProgressChangedEventArgs> syncProgressEventHandler = null, IProgress<int> progress = null, CancellationToken ct = default)
        {
            using ISyncService service = Factories.SyncServiceFactory(client, device);
            if (syncProgressEventHandler != null)
            {
                service.SyncProgressChanged += syncProgressEventHandler;
            }
            service.Pull(path, stream, progress, ct);
        }
    }
}
