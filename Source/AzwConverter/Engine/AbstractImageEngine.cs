using MobiMetadata;
using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter.Engine
{
    public abstract class AbstractImageEngine
    {
        protected MobiMetadata.MobiMetadata Metadata { get; set; }

        protected async Task<(MobiMetadata.MobiMetadata, IDisposable[])> ReadMetadataAsync(FileInfo[] dataFiles)
        {
            var azwFile = dataFiles.First(file => file.IsAzwOrAzw3File());

            //TODO: Handle IOException when Kdle is running.
            var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
            var stream = mappedFile.CreateViewStream();

            var disposables = new IDisposable[] { stream, mappedFile };
            var metadata = MetadataManager.GetConfiguredMetadata();
            try
            {
                await metadata.ReadMetadataAsync(stream);
            }
            catch (MobiMetadataException ex)
            {
                ProgressReporter.Error($"Error reading metadate from {azwFile}.", ex);

                MetadataManager.DisposeDisposables(disposables);
                throw;
            }

            return (metadata, disposables);
        }

        protected async Task<CbzState?> ReadImageDataAsync(string bookId, FileInfo[] dataFiles)
        {
            var metadata = MetadataManager.GetCachedMetadata(bookId);
            IDisposable[]? disposables = null;
            try
            {
                if (metadata == null)
                {
                    (metadata, disposables) = await ReadMetadataAsync(dataFiles);
                }
                Metadata = metadata;
                await metadata.ReadImageRecordsAsync();

                var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResOrAzw6File());
                if (hdContainer != null)
                {
                    using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                    using var hdStream = hdMappedFile.CreateViewStream();

                    await metadata.ReadHDImageRecordsAsync(hdStream);

                    return await ProcessImagesAsync(metadata.PageRecordsHD, metadata.PageRecords);
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    sb.Append(bookId).Append(" / ").Append(metadata.MobiHeader.FullName);
                    sb.Append(": no HD image container");

                    ProgressReporter.Warning(sb.ToString());

                    return await ProcessImagesAsync(null, metadata.PageRecords);
                }
            }
            finally
            {
                if (disposables != null)
                {
                    MetadataManager.DisposeDisposables(disposables);
                }
                else
                {
                    MetadataManager.DisposeCachedMetadata(bookId);
                }
            }
        }
              
        protected abstract Task<CbzState?> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords);
    }
}