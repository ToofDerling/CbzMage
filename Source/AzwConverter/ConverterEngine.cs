using AzwMetadata;
using CbzMage.Shared.Helpers;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter
{
    public class ConverterEngine
    {
        private string? _cbzFile;
        private string? _coverFile;

        public CbzState ScanBook(string bookId, FileInfo[] dataFiles)
        {
            return ConvertBook(bookId, dataFiles, null, null);
        }

        public CbzState SaveCover(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            return ConvertBook(bookId, dataFiles, null, coverFile);
        }

        public CbzState ConvertBook(string bookId, FileInfo[] dataFiles, string? cbzFile, string? coverFile)
        {
            _cbzFile = cbzFile;
            _coverFile = coverFile;

            var azwFile = dataFiles.First(file => file.IsAzwFile());

            using var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
            using var stream = mappedFile.CreateViewStream();

            var metadata = new MobiMetadata(stream);

            var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResFile());
            if (hdContainer != null)
            {
                using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                using var hdStream = hdMappedFile.CreateViewStream();

                metadata.ReadHDImageRecords(hdStream);

                if (_cbzFile != null)
                {
                    return CreateCbz(metadata.PageRecordsHD, metadata.PageRecords);
                }
                else
                {
                    return ReadPages(metadata.PageRecordsHD, metadata.PageRecords);
                }
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.Append(bookId).Append(" / ").Append(metadata.MobiHeader.FullName);
                sb.Append(": no HD image container");

                ProgressReporter.Warning(sb.ToString());

                if (_cbzFile != null)
                {
                    return CreateCbz(null, metadata.PageRecords);
                }
                else
                {
                    return ReadPages(null, metadata.PageRecords);
                }
            }
        }

        private CbzState CreateCbz(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var tempFile = $"{_cbzFile}.temp";
            File.Delete(tempFile);

            var state = ReadAndCompress(tempFile, hdImageRecords, sdImageRecords);

            File.Delete(_cbzFile);
            File.Move(tempFile, _cbzFile);

            return state;
        }

        private CbzState ReadAndCompress(string tempFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            using var zipArchive = ZipFile.Open(tempFile, ZipArchiveMode.Create);
            return ReadAndCompressPages(zipArchive, hdImageRecords, sdImageRecords);
        }

        private CbzState ReadPages(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            // Pass a null ziparchiver to work in readonly mode
            return ReadAndCompressPages(null, hdImageRecords, sdImageRecords);
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
                WriteData(zipArchive, coverName, coverRecord, isCover: true);
            }

            // Pages
            PageRecord pageRecord;
            var firstPage = true;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var page = state.PageName();

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

                WriteData(zipArchive, page, pageRecord, isCover: coverRecord == null && firstPage);
                firstPage = false;
            }

            return state;
        }

        private void WriteData(ZipArchive? zip, string name, PageRecord record, bool isCover)
        {
            // Only read data when we want to use it
            if (zip == null && (_coverFile == null || !isCover))
            {
                return;
            }
            var data = record.ReadData();

            if (isCover && _coverFile != null)
            {
                using var coverStream = File.Open(_coverFile, FileMode.Create, FileAccess.Write);
                coverStream.Write(data);
            }

            if (zip != null)
            {
                var entry = zip.CreateEntry(name, Settings.CompressionLevel);
                using var stream = entry.Open();
                stream.Write(data);
            }
        }
    }
}