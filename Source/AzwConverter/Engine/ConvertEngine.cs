using CbzMage.Shared;
using CbzMage.Shared.Helpers;
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

            var azwFile = dataFiles.First(file => file.IsAzwOrAzw3File());
            _mappedArchiveLen = azwFile.Length;

            var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResOrAzw6File());
            if (hdContainer != null)
            {
                _mappedArchiveLen += hdContainer.Length;
            }

            return await ReadImageDataAsync(bookId, dataFiles);
        }

        public async Task<CbzState> ConvertMetadataAsync(MobiMetadata.MobiMetadata metadata,
            string cbzFile, long dataLen, string coverFile)
        {
            Metadata = metadata;

            _cbzFile = cbzFile;
            _mappedArchiveLen = dataLen;

            _coverFile = coverFile;

            return await CreateCbzAsync(metadata.PageRecordsHD, metadata.PageRecords);
        }

        protected override async Task<CbzState?> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
            => await CreateCbzAsync(pageRecordsHd, pageRecords);

        private async Task<CbzState> CreateCbzAsync(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            var tempFile = $"{_cbzFile}.temp";

            var state = await ReadAndCompressAsync(tempFile, hdImageRecords, sdImageRecords);

            File.Move(tempFile, _cbzFile, overwrite: true);

            return state;
        }

        private async Task<CbzState> ReadAndCompressAsync(string tempFile, PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            CbzState state;
            long realArchiveLen;

            using (var mappedFileStream = AsyncStreams.AsyncFileWriteStream(tempFile))
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

            var foundRealCover = (hdCoverRecord != null || coverRecord != null)
                && await WriteRecordAsync(zipArchive, coverName, state,
                    hdCoverRecord, coverRecord!, isRealCover: true, isFakeCover: false);

            // Pages
            PageRecord? pageRecord;
            PageRecord? hdPageRecord = null;

            for (int i = 0, sz = sdImageRecords.ContentRecords.Count; i < sz; i++)
            {
                state.Pages++;
                var pageName = SharedSettings.GetPageString(state.Pages);

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
            Stream? coverStream = (isRealCover || isFakeCover) && _coverFile != null 
                ? AsyncStreams.AsyncFileWriteStream(_coverFile) 
                : null;

            var entry = zipArchive.CreateEntry(pageName, Settings.CompressionLevel);
            using var stream = entry.Open();

            if (hdRecord != null
                && await hdRecord.TryWriteHDImageDataAsync(stream, coverStream!))
            {
                coverStream?.Dispose();

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
                await record.WriteDataAsync(stream, coverStream!);
                coverStream?.Dispose();

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