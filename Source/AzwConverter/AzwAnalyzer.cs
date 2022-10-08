using AzwConverter.Metadata;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter
{
    public class AzwAnalyzer
    {
        public void Analyze(Dictionary<string, string[]> books, Dictionary<string, string> titles, int numberOfThreads)
        {
            var sWatch = new Stopwatch();
            sWatch.Start();

            var count = 0;

            var directory = Path.Combine(Settings.AzwDir, "Analysis");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var checkForHdContainer = true;

            foreach (var book in books)
            {
                count++;
                var azwPath = book.Value.First(b => b.EndsWith(Settings.AzwExt));
                using var mappedFile = MemoryMappedFile.CreateFromFile(azwPath);
                using var stream = mappedFile.CreateViewStream();

                var writeTitle = true;
                MobiMetadata meta = null;
                try
                {
                    meta = new MobiMetadata(stream);
                }
                catch (Exception ex)
                {
                    if (!titles.TryGetValue(book.Key, out var currentTitle))
                    {
                        currentTitle = book.Key;
                    }
                    Console.WriteLine();
                    Console.WriteLine($"{count}/{books.Count} - {Path.GetFileName(currentTitle)}");
                    Console.WriteLine($"ERROR: {ex.Message}");
                    continue;
                }

                var title = meta.MobiHeader.FullName.ToFileSystemString();
                Console.WriteLine();
                Console.WriteLine($"{count}/{books.Count} - {Path.GetFileName(title)}");
                writeTitle = false;
                
                
                var hdContainer = book.Value.FirstOrDefault(l => l.EndsWith(Settings.AzwResExt));
                if (checkForHdContainer && hdContainer == null)
                {
                    if (writeTitle)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{count}/{books.Count} - {Path.GetFileName(title)}");
                        writeTitle = false;
                    }
                    Console.WriteLine("No HD container.");
                }
                else
                {
                    var hd = book.Value.FirstOrDefault(l => l.EndsWith(Settings.AzwResExt));
                    
                    using var hdMappedFile = MemoryMappedFile.CreateFromFile(hd);
                    using var hdStream = mappedFile.CreateViewStream();
                    
                    meta.ReadHDImageRecords(hdStream);

                //    for (int i = 0; i < meta.PageRecordsHD.ContentRecords.Count; i++)
                //    {
                //        var rec = meta.PageRecordsHD.ContentRecords[i];
                //        rec.TryReadData(out var _);
                //    }
                }
                
                var baseFile = Path.Combine(directory, Path.GetFileName(title));

                if (title.Contains("Split Second Chance"))
                {
                    for (int i = 0; i < meta.PageRecords.RestOfRecords.Count; i++)
                    {
                        var rec = meta.PageRecords.RestOfRecords[i];
                        var file = $"{baseFile}.rest{i}.jpg";

                        var data = meta.PageRecords.RestOfRecords[i].ReadData();
                        File.WriteAllBytes(file, data.ToArray());
                    }

                    var dir = Path.Combine(directory, "pages");
                    Directory.CreateDirectory(dir);

                    //var xml = meta.PageRecords.RescRecord.GetPrettyPrintXml(book.Key);
                    //var xmlFile = Path.Combine(dir, "Resc.xml");
                    //File.WriteAllText(xmlFile, xml);

                    if (meta.PageRecords.CoverRecord != null)
                    {
                        var file = Path.Combine(dir, "cover.jpg");
                        var coverData = meta.PageRecords.CoverRecord.ReadData();
                        File.WriteAllBytes(file, coverData.ToArray());
                    }

                    for (int i = 0; i < meta.PageRecords.RescRecord.Pages.Count; i++)
                    {
                        var page = meta.PageRecords.RescRecord.Pages[i];
                        var file = Path.Combine(dir, $"{page}.jpg");

                        var pageData = meta.PageRecords.ContentRecords[i].ReadData();
                    
                        File.WriteAllBytes(file, pageData.ToArray());
                    }

                    var dirHd = Path.Combine(directory, "pagesHD");
                    Directory.CreateDirectory(dirHd);
                }

                var bookType = meta.MobiHeader.EXTHHeader.BookType;
                if (bookType != "comic")
                {
                    if (writeTitle)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{count}/{books.Count} - {Path.GetFileName(title)}");
                        writeTitle = false;
                    }
                    Console.WriteLine($"BookType is {bookType}");
                }

                if (meta.PageRecords.CoverRecord == null || meta.MobiHeader.EXTHHeader.CoverOffset == 0)
                {
                    if (writeTitle)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"{count}/{books.Count} - {Path.GetFileName(title)}");
                        writeTitle = false;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"PageCount: {meta.PageRecords.RescRecord.Pages.Count} CoverOffset: 0");
                    sb.AppendLine($"ImageCount: {meta.PageRecords.ContentRecords.Count} CoverImage: {meta.PageRecords.CoverRecord != null}");

                    Console.Write(sb.ToString());
                }
            }

            Console.WriteLine(sWatch.ElapsedMilliseconds);
            sWatch.Stop();
        }

    }
}
