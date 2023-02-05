using CbzMage.Shared;
using CbzMage.Shared.IO;
using MobiMetadata;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;

namespace AzwConverter.Engine
{
    public class ConvertBookEngine : AbstractImageEngine
    {
        protected string? _cbzFile;
        protected string? _coverFile;

        protected long _mappedArchiveLen;

        public async Task<CbzState> ConvertBookAsync(string bookId, FileInfo[] dataFiles, string cbzFile, string? coverFile)
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

        protected override async Task<CbzState> ProcessImagesAsync()
            => await CreateCbzAsync();

        protected async Task<CbzState> CreateCbzAsync()
        {
            var tempFile = $"{_cbzFile}.temp";

            var state = await ReadAndCompressAsync(tempFile);

            File.Move(tempFile, _cbzFile!, overwrite: true);

            return state;
        }

        private async Task<CbzState> ReadAndCompressAsync(string tempFile)
        {
            CbzState state;
            long realArchiveLen;

            using (var mappedFileStream = AsyncStreams.AsyncFileWriteStream(tempFile))
            {
                using (var mappedArchive = MemoryMappedFile.CreateFromFile(mappedFileStream, null,
                    _mappedArchiveLen, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None,
                    leaveOpen: true))
                {
                    using (var archiveStream = mappedArchive.CreateViewStream())
                    {
                        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create,
                            leaveOpen: true))
                        {
                            state = await ReadAndCompressPagesAsync(zipArchive);
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

        private async Task<CbzState> ReadAndCompressPagesAsync(ZipArchive zipArchive)
        {
            var state = new CbzState();
            const string coverName = "cover.jpg";

            // Cover
            if (Metadata.MergedCoverRecord != null)
            {
                state.HdCover = Metadata.IsHdCover();
                state.SdCover = !state.HdCover;

                await WriteRecordAsync(zipArchive, coverName, Metadata.MergedCoverRecord, 
                    isRealCover: true, isFakeCover: false);
            }

            // Pages
            for (int pageIndex = 0, sz = Metadata.MergedImageRecords.Count; pageIndex < sz; pageIndex++)
            {
                state.Pages++;

                var pageName = SharedSettings.GetPageString(state.Pages);

                var pageRecord = Metadata.MergedImageRecords[pageIndex];

                if (Metadata.IsHdPage(pageIndex))
                {
                    state.HdImages++;
                }
                else
                { 
                    state.SdImages++;
                }

                var isFakeCover = pageIndex == 0 && !state.HdCover && !state.SdCover;

                await WriteRecordAsync(zipArchive, pageName, pageRecord, isRealCover: false, 
                    isFakeCover: isFakeCover);
            }

            return state;
        }

        private async Task WriteRecordAsync(ZipArchive zipArchive, string pageName,
            PageRecord record, bool isRealCover, bool isFakeCover)
        {
            // Write a cover file?
            Stream? coverStream = (isRealCover || isFakeCover) && _coverFile != null
                ? AsyncStreams.AsyncFileWriteStream(_coverFile)
                : null;

            var entry = zipArchive.CreateEntry(pageName, Settings.CompressionLevel);
            using var stream = entry.Open();

            await record.WriteDataAsync(stream, coverStream!);
            coverStream?.Dispose();
        }
    }
}