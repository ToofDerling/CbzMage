using MobiMetadata;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace AzwConverter.Engine
{
    public class ConvertEngine : AbstractImageEngine
    {
        private string _cbzFile;
        private string? _coverFile;

        private long _mappedArchiveLen;

        public async Task<CbzState> ConvertBookAsync(string bookId, FileInfo[] dataFiles, string cbzFile, string? coverFile)
        {
            _cbzFile = cbzFile;
            _coverFile = coverFile;

            var azwFile = dataFiles.First(file => file.IsAzwFile());
            _mappedArchiveLen = azwFile.Length;

            var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResFile());
            if (hdContainer != null)
            {
                _mappedArchiveLen += hdContainer.Length;
            }

            return await ReadMetaDataAsync(bookId, dataFiles);
        }

        protected override CbzState ProcessImages(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            return CreateCbz(pageRecordsHd, pageRecords);
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
            CbzState state;
            long realArchiveLen;

            using var mappedFileStream = new FileStream(tempFile, FileMode.CreateNew);

            using (var mappedArchive = MemoryMappedFile.CreateFromFile(mappedFileStream, null,
                _mappedArchiveLen, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            {
                using var archiveStream = mappedArchive.CreateViewStream();
                
                using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    state = ReadAndCompressPages(zipArchive, hdImageRecords, sdImageRecords);
                }
                
                realArchiveLen = archiveStream.Position;
            }

            if (mappedFileStream.Length != realArchiveLen)
            {
                mappedFileStream.SetLength(realArchiveLen);
            }

            return state;
        }

        private CbzState ReadAndCompressPages(ZipArchive? zipArchive, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState();

            // Cover
            PageRecord coverRecord = null;
            const string coverName = "cover.jpg";

            // First try HD cover
            state.HdCover = hdImageRecords != null && hdImageRecords.CoverRecord != null && hdImageRecords.CoverRecord.IsCresRecord;
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
                WriteData(coverName, coverRecord, isCoverRecord: true);
            }

            // Pages
            PageRecord pageRecord;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var page = state.PageName();

                // First try the HD image
                if (hdImageRecords != null && hdImageRecords.ContentRecords[i].IsCresRecord)
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

                // If we don't have a proper cover record use the first page
                var isCoverRecord = coverRecord == null && i == 0;

                WriteData(page, pageRecord, isCoverRecord: isCoverRecord);
            }

            return state;

            void WriteData(string name, PageRecord record, bool isCoverRecord)
            {
                var entry = zipArchive.CreateEntry(name, Settings.CompressionLevel);
                using var stream = entry.Open();

                var data = record.ReadData();
                stream.Write (data);

                if (isCoverRecord && _coverFile != null)
                {
                    SaveFile(data, _coverFile);
                }
            }
        }
    }
}