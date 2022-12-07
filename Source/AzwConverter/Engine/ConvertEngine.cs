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

        public async Task<CbzState?> ConvertBookAsync(string bookId, FileInfo[] dataFiles, string cbzFile, string? coverFile)
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

        protected override async Task<CbzState?> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
            => await CreateCbzAsync(pageRecordsHd, pageRecords);

        private async Task<CbzState> CreateCbzAsync(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var tempFile = $"{_cbzFile}.temp";
            File.Delete(tempFile);

            var state = await ReadAndCompressAsync(tempFile, hdImageRecords, sdImageRecords);

            File.Delete(_cbzFile);
            File.Move(tempFile, _cbzFile);

            return state;
        }

        private async Task<CbzState> ReadAndCompressAsync(string tempFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            CbzState state;
            long realArchiveLen;

            using (var mappedFileStream = new FileStream(tempFile, FileMode.CreateNew))
            {
                using (var mappedArchive = MemoryMappedFile.CreateFromFile(mappedFileStream, null,
                    _mappedArchiveLen, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                {
                    using (var archiveStream = mappedArchive.CreateViewStream())
                    {
                        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                        {
                            state = await ReadAndCompressPagesAsync(zipArchive, hdImageRecords, sdImageRecords);
                        }

                        realArchiveLen = archiveStream.Position;
                    }
                }

                if (mappedFileStream.Length != realArchiveLen)
                {
                    mappedFileStream.SetLength(realArchiveLen);
                }
            }

            return state;
        }

        private async Task<CbzState> ReadAndCompressPagesAsync(ZipArchive zipArchive, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var state = new CbzState();
            const string coverName = "cover.jpg";

            // Cover
            PageRecord? coverRecord;
            PageRecord? hdCoverRecord = null;

            if (hdImageRecords != null)
            {
                hdCoverRecord = hdImageRecords.CoverRecord ?? null;
            }
            coverRecord = sdImageRecords.CoverRecord;

            var foundRealCover = false;

            if (hdCoverRecord != null || coverRecord != null)
            {
                foundRealCover = await WriteRecordAsync(zipArchive, coverName, state, hdCoverRecord, coverRecord, 
                    isRealCover: true, isFakeCover: false);
            }

            // Pages
            PageRecord? pageRecord;
            PageRecord? hdPageRecord = null;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var pageName = state.PageName();

                if (hdImageRecords != null)
                {
                    hdPageRecord = hdImageRecords.ContentRecords[i];
                }
                pageRecord = sdImageRecords.ContentRecords[i];

                var isFakeCover = !foundRealCover && i == 0;

                await WriteRecordAsync(zipArchive, pageName, state, hdPageRecord, pageRecord,
                    isRealCover: false, isFakeCover: isFakeCover);
            }

            return state;

        }

        private async Task<bool> WriteRecordAsync(ZipArchive zipArchive, string pageName, CbzState state, 
            PageRecord? hdRecord, PageRecord record, bool isRealCover, bool isFakeCover)
        {
            // Write a cover file?
            var coverFile = (isRealCover || isFakeCover) && _coverFile != null ? _coverFile : null;

            var entry = zipArchive.CreateEntry(pageName, Settings.CompressionLevel);
            using var stream = entry.Open();

            if (hdRecord != null
                && await hdRecord.WriteDataAsync(stream, ImageRecordHD.RecordId, coverFile))
            {
                if (isRealCover)
                {
                    state.HdCover = true;
                }
                else
                {
                    state.HdImages++;
                }
                return true;
            }

            if (record != null)
            {
                await record.WriteDataAsync(stream, file: coverFile);
                if (isRealCover)
                {
                    state.SdCover = true;
                }
                else
                {
                    state.SdImages++;
                }
                return true;
            }

            return false;
        }
    }
}