using MobiMetadata;
using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;

namespace AzwConverter.Engine
{
    public abstract class AbstractImageEngine
    {
        protected MobiMetadata.MobiMetadata? Metadata { get; set; }

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

        protected async Task<CbzState> ReadImageDataAsync(string bookId, params FileInfo[] dataFiles)
        {
            var metadata = GetCachedMobiMetadata(bookId);

            IDisposable[]? disposables = null;
            try
            {
                if (metadata == null)
                {
                    (metadata, disposables) = await ReadMetadataAsync(dataFiles);
                }
                Metadata = metadata;
                await metadata.ReadImageRecordsAsync();

                var hdContainer = SelectHDContainer(dataFiles);
                if (hdContainer != null)
                {
                    using var hdMappedFile = MemoryMappedFile.CreateFromFile(hdContainer.FullName);
                    using var hdStream = hdMappedFile.CreateViewStream();

                    await metadata.ReadHDImageRecordsAsync(hdStream);

                    return await ProcessImagesAsync(metadata.PageRecordsHD, metadata.PageRecords);
                }
                else  
                {
                    DisplayHDContainerWarning(bookId, metadata.MobiHeader.GetFullTitle());

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

        protected virtual MobiMetadata.MobiMetadata? GetCachedMobiMetadata(string bookId)
        {
            return MetadataManager.GetCachedMetadata(bookId);
        }

        protected virtual FileInfo? SelectHDContainer(FileInfo[] dataFiles)
        { 
            return dataFiles.FirstOrDefault(file => file.IsAzwResOrAzw6File());
        }

        protected virtual void DisplayHDContainerWarning(string fileName, string title)
        {
            ProgressReporter.Warning(
                $"{Environment.NewLine}[{fileName}] / [{title}]: no HD image container");
        }

        protected abstract Task<CbzState> ProcessImagesAsync(PageRecords? pageRecordsHd, PageRecords pageRecords);
    }
}