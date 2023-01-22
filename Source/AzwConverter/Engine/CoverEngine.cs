using CbzMage.Shared.IO;
using MobiMetadata;

namespace AzwConverter.Engine
{
    public class CoverEngine : AbstractImageEngine
    {
        private string? _coverFile;
        private string? _coverString;

        public async Task<CbzState> SaveCoverAsync(string bookId, FileInfo[] dataFiles, string coverFile)
        {
            _coverFile = coverFile;

            return await ReadImageDataAsync(bookId, dataFiles);
        }

        public string? GetCoverString()
        { 
            return _coverString;
        }

        protected override async Task<CbzState> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords)
        {
            await SaveCoverAsync(pageRecordsHd, pageRecords);
            return new CbzState();
        }

        private async Task SaveCoverAsync(PageRecords? hdImageRecords, PageRecords sdImageRecords)
        {
            using var stream = AsyncStreams.AsyncFileWriteStream(_coverFile!);

            // First try HD cover
            if (hdImageRecords != null && hdImageRecords.CoverRecord != null 
                && await hdImageRecords.CoverRecord.TryWriteHDImageDataAsync(stream))
            {
                _coverString = "HD cover";
            }
            // Then the SD cover
            else if (sdImageRecords.CoverRecord != null)
            {
                await sdImageRecords.CoverRecord.WriteDataAsync(stream);
                _coverString = "SD cover";
            }
            // Then the first HD page
            else if (hdImageRecords != null && hdImageRecords.ContentRecords.Count > 0
                && await hdImageRecords.ContentRecords[0].TryWriteHDImageDataAsync(stream))
            {
                _coverString = "HD page 1";
            }
            // Then the first SD page
            else
            {
                await sdImageRecords.ContentRecords[0].WriteDataAsync(stream);
                _coverString = "SD page 1";
            }
        }
    }
}