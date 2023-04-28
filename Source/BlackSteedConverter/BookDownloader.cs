using AdbClient;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlackSteedConverter
{
    internal class BookDownloader
    {
        private const string _tmpPath = "M:\\Media\\Cbz";

        private static readonly string[] _booksPaths = new[]
        {
            "/storage/emulated/0/Android/data/com.darkhorse.digital/files/books",
            "/storage/self/primary/Android/data/com.darkhorse.digital/files/books",
            "/sdcard/Android/data/com.darkhorse.digital/files/books"
        };

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

        private async Task<IList<StatEntry>> ListDirAsync(string dir, Predicate<StatEntry>? filter = null)
        {
            var entries = await _syncClient.List(dir);
            if (entries.Count > 0)
            {
                entries = entries.Where(entry => entry.FullPath != "." && entry.FullPath != ".."
                        && (filter == null || filter!(entry))).ToList();
            }
            return entries;
        }

        private readonly Predicate<StatEntry> _getBookDirsFilter = entry => entry.FullPath.IsBookDirectory();
        private readonly Predicate<StatEntry> _getBookFilesAndManifestFilter = entry => entry.FullPath.IsBookFile() || entry.FullPath == "manifest.json";

        private readonly Predicate<StatEntry> _getDirsFilter = entry =>
            ((entry.Mode & UnixFileMode.Directory) == UnixFileMode.Directory || (entry.Mode & UnixFileMode.SymLink) == UnixFileMode.SymLink)
                && entry.FullPath != "proc";

        public async Task<bool> TryBooksPathsAsync()
        {
            var stopwatch = new Stopwatch();

            foreach (var booksPath in _booksPaths)
            {
                Console.WriteLine($"Trying {booksPath}");

                stopwatch.Restart();
                var res = await AnalyzeBooksDirAsync(booksPath);
                stopwatch.Stop();
                Console.WriteLine(stopwatch.ElapsedMilliseconds);

                if (res)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> AnalyzeBooksDirAsync(string dir)
        {
            var bookList = await ListDirAsync(dir, _getBookDirsFilter);
            Console.WriteLine($"Checking {bookList.Count} book{bookList.SIf1()}");

            var allGood = 0;
            var fileCount = 0;
            var totalFileSize = 0L;




            //await Parallel.ForEachAsync(bookList, Settings.ParallelOptions, async (book, _) =>

                foreach (var book in bookList)
            {
                var bookPath = $"{dir}/{book.FullPath}";
                var files = await ListDirAsync(bookPath, _getBookFilesAndManifestFilter);

                if (files.Count == 0)
                {
                    allGood++;
                    Console.WriteLine($"Found empty book: {bookPath}");
                }
                else if (!files.Any(file => file.FullPath == "manifest.json"))
                {
                    allGood++;
                    Console.WriteLine($"Found book without manifest: {bookPath}");
                }
                else if (files.Count == 1)
                {
                    allGood++;
                    Console.WriteLine($"Found book with just a manifest: {bookPath}");
                }

                fileCount += files.Count;
                totalFileSize += files.Sum(file => file.Size);
            }
                //);

            if (allGood == 0)
            {
                Console.WriteLine("All good.");
                Console.WriteLine($"Files: {fileCount}");
                Console.WriteLine($"Size: {totalFileSize / (1024 * 1024)} MB");
            }

            return allGood == 0;
        }

        private bool _findAllBooksDirs = false;

        public async Task<bool> FindBooksDirAsync(string rootDir = "/")
        {
            const string blackSteedDir = "com.darkhorse.digital/files/books";

            var dirs = await ListDirAsync(rootDir, _getDirsFilter);

            var separator = rootDir == "/" ? string.Empty : "/";

            foreach (var dir in dirs)
            {
                var newRootDir = $"{rootDir}{separator}{dir.FullPath}";

                if (newRootDir.EndsWith(blackSteedDir) && await AnalyzeBooksDirAsync(newRootDir)
                    && !_findAllBooksDirs)
                {
                    Console.WriteLine($"Found {newRootDir}");
                    return true;
                }

                if (await FindBooksDirAsync(newRootDir) && !_findAllBooksDirs)
                {
                    return true;
                }
            }

            return false;
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

            var books = await _syncClient.List(_booksPaths[0]);

            foreach (var book in books)
            //Parallel.ForEach(books, Settings.ParallelOptions, book =>
            {
                if ((book.Mode & UnixFileMode.Directory) != UnixFileMode.Directory)
                {
                    continue;
                    //return;
                }

                if (!book.FullPath.IsBookDirectory())
                {
                    continue;
                    //return;
                }

                var bookPath = $"{_booksPaths[0]}/{book}";
                var files = await _syncClient.List(bookPath);

                long size = 0;
                var pages = new List<StatEntry>();

                var foundManifest = false;

                foreach (var file in files)
                {
                    if (file.Size > 0)
                    {
                        if (file.FullPath == "manifest.json")
                        {
                            foundManifest = true;
                            pages.Add(file);
                            size += file.Size;
                        }
                        else if (file.FullPath.IsBookFile())
                        {
                            pages.Add(file);
                            size += file.Size;
                        }
                    }
                }

                if (!foundManifest)
                {
                    ProgressReporter.Warning($"{book.FullPath}: No manifest found");
                    Console.WriteLine();
                }
                if (pages.Count == 0)
                {
                    ProgressReporter.Warning($"{book.FullPath}: No pages found");
                    Console.WriteLine();
                }

                if (foundManifest && pages.Count > 0)
                {
                    const int mb = 1024 * 1024;

                    var sb = new StringBuilder(book.FullPath).AppendLine();

                    sb.Append(pages.Count).Append(" pages (").Append(size / mb).Append(" MB)").AppendLine();

                    Console.WriteLine(sb.ToString());

                    //ProgressReporter.Info($"{pages.Count} pages ({size / (1024 * 1024)} MB)");
                    booksDict.Add(book.FullPath, pages);
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
