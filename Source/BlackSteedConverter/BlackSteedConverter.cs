using CbzMage.Shared;
using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using System.IO.Compression;
using System.Text.Json;

namespace BlackSteedConverter
{
    public class BlackSteedConverter
    {
        public async Task ConvertDirectoryAsync(string bookDir)
        {

            var config = new BlackSteedConvertSettings();
            config.CreateSettings();
            
            var downloader = new BookDownloader();

            if (!await downloader.StartSyncServerAsync())
            {
                return;
            }

            //downloader.UploadFile();

            await downloader.GetBooksAsync();

            return;


            if (string.IsNullOrEmpty(bookDir)) 
            {
                bookDir = Environment.CurrentDirectory;
            }

            if (!Directory.Exists(bookDir))
            {
                ProgressReporter.Error($"Directory not found: {bookDir}");
                return;
            }


            var blackSteedBooks = GetBooks(bookDir);

            Console.WriteLine($"Found {blackSteedBooks.Count} book{blackSteedBooks.SIf1()}");
            Console.WriteLine();

            foreach (var blackSteedBook in blackSteedBooks)
            {
                try
                {
                    CreateCbz(blackSteedBook);
                }
                catch (Exception ex)
                {
                    ProgressReporter.Error("Error creating Cbz:", ex);
                    Console.WriteLine();
                }
            }

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private List<(string, string)> GetBooks(string bookDir)
        {
            var books = new List<(string, string)>();

            // Is it a single book we're converting?
            var book = GetBook(bookDir);
            if (book != default)
            {
                books.Add(book);
            }
            else
            {
                // Else scan for books...
                var dirs = Directory.GetDirectories(bookDir);
                foreach (var dir in dirs)
                {
                    book = GetBook(dir);
                    if (book != default)
                    {
                        books.Add(book);
                    }
                }
            }

            return books;
        }

        private (string dir, string manifest) GetBook(string dir)
        {
            var name = Path.GetFileName(dir);

            if (Guid.TryParseExact(name, "N", out var _))
            {
                var manifest = Path.Combine(dir, "manifest.json");
                if (File.Exists(manifest))
                {
                    return (dir, manifest);
                }
            }
            return default;
        }

        private void CreateCbz((string, string) blackSteedBook)
        {
            var (dir, manifest) = blackSteedBook;

            Console.WriteLine(Path.GetFileName(dir));

            var json = File.ReadAllText(manifest);
            var book = JsonSerializer.Deserialize<Book>(json);

            VerifyPageOrder(book);

            var pages = GetPages(book, dir);

            Console.WriteLine($"{pages.Count} page{pages.SIf1()}");

            var cbz = GetCbz(dir);

            CompressPages(pages, cbz);

            Console.WriteLine();
        }

        private string GetCbz(string dir)
        {
            var cbz = $"{dir}.cbz";

            if (!string.IsNullOrEmpty(Settings.CbzDir))
            {
                cbz = Path.Combine(Settings.CbzDir, Path.GetFileName(cbz));
            }
            File.Delete(cbz);

            ProgressReporter.Done(Path.GetFileName(cbz));

            return cbz;
        }

        private void VerifyPageOrder(Book book)
        {
            var prevSortOrder = 0;

            foreach (var page in book.pages)
            {
                if (page.sort_order <= prevSortOrder)
                {
                    throw new ApplicationException($"sort_order {page.sort_order} vs prevSortOrder {prevSortOrder} mismatch. Check manifest");
                }
                prevSortOrder = page.sort_order;
            }
        }

        private List<string> GetPages(Book book, string bookDir)
        {
            var pages = new List<string>();
            var pageCount = 0;

            foreach (var page in book.pages)
            {
                pageCount++;

                var from = Path.Combine(bookDir, page.src_image);

                var jpg = SharedSettings.GetPageString(pageCount);
                var to = Path.Combine(bookDir, jpg);

                if (!File.Exists(to))
                {
                    File.Move(from, to);
                }

                pages.Add(to);
            }

            return pages;
        }

        private void CompressPages(List<string> pages, string cbz)
        {
            var reporter = new ProgressReporter(pages.Count);

            using var archive = ZipFile.Open(cbz, ZipArchiveMode.Create);

            var firstPage = true;

            foreach (var page in pages)
            {
                var name = Path.GetFileName(page);
                var entry = archive.CreateEntry(name, Settings.CompressionLevel);

                using var to = entry.Open();
                using var from = File.OpenRead(page);

                from.CopyTo(to);
                reporter.ShowProgress(name);

                if (firstPage)
                {
                    firstPage = false;

                    if (Settings.SaveCover)
                    {
                        SaveCover(cbz, from);
                    }
                }
            }

            Console.WriteLine();
        }

        private void SaveCover(string cbz, Stream image)
        {
            var cover = Path.ChangeExtension(cbz, ".jpg");
            File.Delete(cover);

            image.Position = 0;

            using var coverStream = File.OpenWrite(cover);
            image.CopyTo(coverStream);
        }

        class Page
        {
            public int sort_order { get; set; }
            public string src_image { get; set; }
        }

        class Book
        {
            public Page[] pages { get; set; }
        }
    }
}
