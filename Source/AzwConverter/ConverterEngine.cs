using AzwConverter.Metadata;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter
{
    public class ConverterEngine
    {
        private readonly AzwAction _azwAction;

        public ConverterEngine(AzwAction azwAction)
        {
            _azwAction = azwAction;
        }

        public CbzState ScanBook(string bookId, string[] dataFiles)
        {
            return ConvertBook(bookId, dataFiles, null);
        }

        public CbzState ConvertBook(string bookId, string[] dataFiles, string cbzFile)
        {
            var azwFile = dataFiles.First(b => b.EndsWith(Settings.AzwExt));

            using var mappedFile = MemoryMappedFile.CreateFromFile(azwFile);
            using var stream = mappedFile.CreateViewStream();

            var metadata = new MobiMetadata(stream);

            var hdContainer = dataFiles.FirstOrDefault(l => l.EndsWith(Settings.AzwResExt));
            if (hdContainer != null)
            {
                using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer);
                using var hdStream = hdMappedFile.CreateViewStream();

                metadata.ReadHDImageRecords(hdStream);
                if (_azwAction == AzwAction.Convert)
                {
                    return CreateCbz(cbzFile, metadata.PageRecordsHD, metadata.PageRecords);
                }
                else
                {
                    return ReadAndCompressPages(null, metadata.PageRecordsHD, metadata.PageRecords);
                }
            }
            else
            {
                // TODO: ProgressReporter.Warn
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.Append(bookId).Append(" / ").Append(metadata.MobiHeader.FullName);
                sb.Append(": no HD image container");
                Console.WriteLine(sb.ToString());

                if (_azwAction == AzwAction.Convert)
                {
                    return CreateCbz(cbzFile, null, metadata.PageRecords);
                }
                else
                {
                    return ReadAndCompressPages(null, null, metadata.PageRecords);
                }
            }
        }

        private CbzState CreateCbz(string cbzFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var tempFile = $"{cbzFile}.temp";
            File.Delete(tempFile);

            var state = ReadAndCompress(tempFile, hdImageRecords, sdImageRecords);

            File.Delete(cbzFile);
            File.Move(tempFile, cbzFile);

            return state;
        }

        private CbzState ReadAndCompress(string cbzFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            using var zipArchive = ZipFile.Open(cbzFile, ZipArchiveMode.Create);
            return ReadAndCompressPages(zipArchive, hdImageRecords, sdImageRecords);
        }

        private CbzState ReadAndCompressPages(ZipArchive? zipArchive, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState();

            // Cover
            PageRecord coverRecord = null;
            const string coverName = "cover.jpg";

            // First try HD cover
            state.HdCover = hdImageRecords != null && hdImageRecords.CoverRecord != null && hdImageRecords.CoverRecord.IsCresRecord();
            if (state.HdCover)
            {
                coverRecord = hdImageRecords.CoverRecord;

            }
            // Then the SD cover
            else if ((state.SdCover = sdImageRecords.CoverRecord != null))
            {
                coverRecord = sdImageRecords.CoverRecord;
            }
            if (coverRecord != null)
            {
                Write(zipArchive, coverName, coverRecord);
            }

            // Pages
            PageRecord pageRecord;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var page = Path.Combine($"page-{state.Pages.ToString().PadLeft(4, '0')}.jpg");


                // First try the HD image
                if (hdImageRecords != null && hdImageRecords.ContentRecords[i].IsCresRecord())
                {
                    state.HdImages++;
                    pageRecord = hdImageRecords.ContentRecords[i];

                }
                // Else merge in the SD image
                else
                {
                    state.SdImages++;
                    pageRecord = sdImageRecords.ContentRecords[i];
                }
                Write(zipArchive, page, pageRecord);
            }

            return state;

            static void Write(ZipArchive? zip, string name, PageRecord record)
            {
                if (zip != null)
                {
                    var entry = zip.CreateEntry(name, Settings.CompressionLevel);
                    using var stream = entry.Open();
                    stream.Write(record.ReadData());
                }
            }
        }
    }
}