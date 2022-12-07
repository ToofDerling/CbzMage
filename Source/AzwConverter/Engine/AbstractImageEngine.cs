using MobiMetadata;
using CbzMage.Shared.Helpers;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AzwConverter.Engine
{
    public abstract class AbstractImageEngine
    {
        protected async Task<CbzState?> ReadMetaDataAsync(string bookId, FileInfo[] dataFiles)
        {
            var metadata = MetadataManager.GetCachedMetadata(bookId);
            IDisposable[] disposables = null;

            try
            {
                if (metadata == null)
                {
                    var azwFile = dataFiles.First(file => file.IsAzwFile());

                    //TODO: Handle IOException when Kdle is running.
                    var mappedFile = MemoryMappedFile.CreateFromFile(azwFile.FullName);
                    var stream = mappedFile.CreateViewStream();

                    disposables = new IDisposable[] { stream, mappedFile };

                    metadata = MetadataManager.ConfigureFullMetadata();
                    await metadata.ReadMetadataAsync(stream);
                }

                await metadata.ReadImageRecordsAsync();

                var hdContainer = dataFiles.FirstOrDefault(file => file.IsAzwResFile());
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
                    MetadataManager.Dispose(disposables);
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