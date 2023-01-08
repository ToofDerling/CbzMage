using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using CbzMage.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlackSteedConverter
{
    internal class BookDownloader
    {
        private const string _adbPath = "C:\\System\\Apps\\Adb\\adb.exe";

        private const string _tmpPath = "M:\\Media\\Cbz";

        private const string _booksPath = "/sdcard/Android/data/com.darkhorse.digital/files/books";

        private IAdbClient _adbClient;

        private DeviceData _device;

        public bool StartSyncServer()
        {
            var server = AdbServer.Instance;
            var status = server.GetStatus();

            if (!status.IsRunning)
            {
                server = new AdbServer();
                var result = server.StartServer(_adbPath, false);
                if (result != StartServerResult.Started)
                {
                    Console.WriteLine("Can't start adb server");
                }
            }
            else
            {
                //server.RestartServer();
            }

            _adbClient = new AdbClient();
            _adbClient.Connect("127.0.0.1:62001");

            _device = _adbClient.GetDevices().FirstOrDefault(); // Get first connected device

            if (_device == null)
            {
                return false;
            }
            return true;
        }

        public void GetBooks()
        {
            var booksDict = new Dictionary<string, List<FileStatistics>>();

            var books = _adbClient.List(_device, _booksPath);

            foreach (var book in books)
            {
                if (!book.Path.IsBookDirectory())
                {
                    continue;
                }
                Console.WriteLine(book.Path);

                var bookPath = $"{_booksPath}/{book}";
                var files = _adbClient.List(_device, bookPath);

                long size = 0;
                var pages = new List<FileStatistics>();

                var foundManifest = false;

                var pageCount = 0;

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
                    ProgressReporter.Warning("No manifest found");
                }
                if (pages.Count == 0)
                {
                    ProgressReporter.Warning("No pages found");
                }

                if (foundManifest && pages.Count > 0)
                {
                    ProgressReporter.Info($"{pages.Count} pages ({size / (1024 * 1024)} MB)");
                    booksDict.Add(book.Path, pages);
                }

                Console.WriteLine();
            }

            DownloadBooks(booksDict);
        }

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

                foreach (var page in book.Value)
                {
                    var pageFile = Path.Combine(bookDir, page.Path);
                    using var stream = File.Create(pageFile);

                    var remoteFile = $"{_booksPath}/{book.Key}/{page.Path}";

                    _adbClient.Pull(_device, remoteFile, stream);
                    reporter.ShowProgress(page.Path);

                }
                Console.WriteLine();

                break;
            }
        }
    }
}
