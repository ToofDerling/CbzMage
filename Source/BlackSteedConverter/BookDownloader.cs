using AdbClient;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlackSteedConverter
{
    internal class BookDownloader
    {
        private const string _tmpPath = "M:\\Media\\Cbz";

        private const string _booksPath = "/sdcard/Android/data/com.darkhorse.digital/files/books";

        private AdbServicesClient _adbClient;

        private (string serial, string state) _device;

        private AdbSyncClient _syncClient;

        public async Task<bool> StartSyncServerAsync()
        {
            if (string.IsNullOrEmpty(Settings.AdbPath))
            {
                return false;
            }

            if (!File.Exists(Settings.AdbPath))
            {
                ProgressReporter.Error($"AdbPath not found: {Settings.AdbPath}");
                return false;
            }

            var adbProcess = Process.GetProcessesByName("adb");
            if (adbProcess == null || adbProcess.Length == 0)
            {
                ProgressReporter.Info("Starting Adb server:");
                Process.Start(Settings.AdbPath, "start-server");
            }
            else
            {
                ProgressReporter.Info("Adb server is running");
            }

            _adbClient = new AdbServicesClient();
            var devices = await _adbClient.GetDevices();
            if (devices == null || devices.Count == 0)
            {
                ProgressReporter.Error($"Found no connected devices");
                return false;
            }

            _device = devices[0];

            if (devices.Count == 1)
            {
                ProgressReporter.Info($"Found {devices.Count} connected device{devices.SIf1()}: {_device.serial}");
            }
            else
            {
                ProgressReporter.Info($"Found {devices.Count} connected device{devices.SIf1()}. Using: {_device.serial}");
            }

            _syncClient = await _adbClient.GetSyncClient(_device.serial);

            return true;
        }

        //public void UploadFile()
        //{
        //    //service.Push(stream, "/data/local/tmp/MyFile.txt", 777, DateTimeOffset.Now, null, CancellationToken.None);

        //    var localFile = "C:\\System\\Projects\\CbzMageExternal\\TestData\\test.jpg";
        //    var name = Path.GetFileName(localFile);

        //    var remotePath1 = $"/data/local/tmp/{name}";
        //    var stat = _adbClient.Stat(_device, remotePath1);

        //    var remotePathNotExists = $"{_booksPath}/test2";
        //    var statNotExists = _adbClient.Stat(_device, remotePathNotExists);

        //    using var stream = File.OpenRead(localFile);

        //    if (statNotExists.FileMode == 0)
        //    {
        //        _adbClient.Push(_device, remotePathNotExists, stream, 777, DateTimeOffset.Now);
        //    }

        //    stream.Position = 0;

        //    var remotePath2 = $"{_booksPath}/{name}";
        //    _adbClient.Push(_device, remotePath2, stream, 777, DateTimeOffset.Now);
        //}



        public async Task GetBooksAsync()
        {
            var booksDict = new Dictionary<string, List<StatEntry>>();

            var books = await _syncClient.List(_booksPath);

            foreach (var book in books)
            //Parallel.ForEach(books, Settings.ParallelOptions, book =>
            {
                if ((book.Mode & UnixFileMode.Directory) != UnixFileMode.Directory)
                {
                    continue;
                    //return;
                }

                if (!book.Path.IsBookDirectory())
                {
                    continue;
                    //return;
                }

                var bookPath = $"{_booksPath}/{book}";
                var files = await _syncClient.List(bookPath);

                long size = 0;
                var pages = new List<StatEntry>();

                var foundManifest = false;

                foreach (var file in files)
                {
                    if (file.Size > 0)
                    {
                        if (file.Path == "manifest.json")
                        {
                            foundManifest = true;
                            pages.Add(file);
                            size += file.Size;
                        }
                        else if (file.Path.IsBookFile())
                        {
                            pages.Add(file);
                            size += file.Size;
                        }
                    }
                }

                if (!foundManifest)
                {
                    ProgressReporter.Warning($"{book.Path}: No manifest found");
                    Console.WriteLine();
                }
                if (pages.Count == 0)
                {
                    ProgressReporter.Warning($"{book.Path}: No pages found");
                    Console.WriteLine();
                }

                if (foundManifest && pages.Count > 0)
                {
                    const int mb = 1024 * 1024;

                    var sb = new StringBuilder(book.Path).AppendLine();

                    sb.Append(pages.Count).Append(" pages (").Append(size / mb).Append(" MB)").AppendLine();

                    Console.WriteLine(sb.ToString());

                    //ProgressReporter.Info($"{pages.Count} pages ({size / (1024 * 1024)} MB)");
                    booksDict.Add(book.Path, pages);
                }

                //});
            }

            //DownloadBooks(booksDict);
        }

        /*
        public void DownloadBooks(Dictionary<string, List<FileStatistics>> booksDict)
        {
            foreach (var book in booksDict)
            {
                var bookDir = Path.Combine(_tmpPath, book.Key);
                if (Directory.Exists(bookDir))
                {
                    Directory.Delete(bookDir, true);
                }
                Directory.CreateDirectory(bookDir);

                var reporter = new ProgressReporter(book.Value.Count);

                //foreach (var page in book.Value)
                Parallel.ForEach(book.Value, Settings.ParallelOptions, page =>
                {
                    var pageFile = Path.Combine(bookDir, page.Path);
                    using var stream = File.Create(pageFile);

                    var remoteFile = $"{_booksPath}/{book.Key}/{page.Path}";

                    _adbClient.Pull(_device, remoteFile, stream);
                    reporter.ShowProgress(page.Path);
                });

                Console.WriteLine();
                break;
            }
        }
        */
    }
}
