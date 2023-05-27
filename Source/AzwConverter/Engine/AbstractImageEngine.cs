using MobiMetadata;
using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;

namespace AzwConverter.Engine
{
    public abstract class AbstractImageEngine
    {
        protected MobiMetadata.MobiMetadata Metadata { get; set; }

        protected bool IgnoreHDContainerWarning { get; set; }

        protected async Task<(MobiMetadata.MobiMetadata, IDisposable[])> ReadMetadataAsync(FileInfo[] dataFiles)
        {
            var azwFile = dataFiles.First(file => file.IsAzwOrAzw3File());

            MemoryMappedFile? mappedFile = null;
            try
            {
                mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
            }
            catch (IOException)
            {
                ProgressReporter.Warning($"Error reading [{azwFile.FullName}] is Kdl running?");
                throw;
            }

            var stream = mappedFile.CreateViewStream();

            var disposables = new IDisposable[] { stream, mappedFile };
            var metadata = MetadataManager.GetConfiguredMetadata();
            try
            {
                await metadata.ReadMetadataAsync(stream);
            }
            catch (MobiMetadataException ex)
            {
                ProgressReporter.Error($"Error reading metadate from [{azwFile}]", ex);

                MetadataManager.DisposeDisposables(disposables);
                throw;
            }

            return (metadata, disposables);
        }

        protected async Task<CbzItem> ReadImageDataAsync(string bookId, params FileInfo[] dataFiles)
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

                var hdContainer = SelectHDContainer(dataFiles);
                if (hdContainer != null)
                {
                    using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                    using var hdStream = hdMappedFile.CreateViewStream();

                    await metadata.SetImageRecordsAsync(hdStream);

                    return await ProcessImagesAsync();
                }
                else
                {
                    await metadata.SetImageRecordsAsync(null);

                    if (!IgnoreHDContainerWarning)
                    {
                        ProgressReporter.Warning(
                            $"{Environment.NewLine}[{bookId}] / [{metadata.MobiHeader.GetFullTitle()}]: no HD image container");
                    }

                    return await ProcessImagesAsync();
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

        protected virtual FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        {
            return dataFiles.FirstOrDefault(file => file.IsAzwResOrAzw6File());
        }

        protected abstract Task<CbzItem> ProcessImagesAsync();
    }
}