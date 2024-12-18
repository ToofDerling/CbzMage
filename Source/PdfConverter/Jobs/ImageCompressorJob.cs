using CbzMage.Shared.Helpers;
using CbzMage.Shared.IO;
using CbzMage.Shared.JobQueue;
using PdfConverter.ImageConversion;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJobConsumer<IEnumerable<int>>
    {
        private readonly ZipArchive? _compressor;

        private readonly List<AbstractImageConverter> _imageProducers;

        private readonly ProgressReporter _progressReporter;

        private readonly string? _savedPage;

        private readonly string? _coverFile;

        public ImageCompressorJob(ZipArchive? compressor, List<AbstractImageConverter> imageProducers, ProgressReporter progressReporter, string? coverFile = null)
        {
            _compressor = compressor;

            _imageProducers = imageProducers;

            _progressReporter = progressReporter;

            _coverFile = coverFile;
        }

        public async Task<IEnumerable<int>> ConsumeAsync()
        {
            var firstPage = true;

            foreach (var producer in _imageProducers)
            {
                if (firstPage)
                {
                    if (_coverFile != null)
                    {
                        using var coverStream = AsyncStreams.AsyncFileWriteStream(_coverFile);
                        await CopyToStreamAsync(coverStream, close: false);
                    }
                    firstPage = false;
                }

                if (_compressor != null)
                {
                    var page = producer.GetPageString();
                    var entry = _compressor.CreateEntry(page);

                    using var cbzStream = entry.Open();
                    await CopyToStreamAsync(cbzStream, close: true);

                    _progressReporter.ShowProgress($"Converted {page}");
                }

                async Task CopyToStreamAsync(Stream stream, bool close)
                {
                    if (producer.ConvertedImageData != null)
                    {
                        await stream.WriteAsync(producer.ConvertedImageData.WrittenMemory);
                    }
                    else
                    {
                        using var imageStream = producer.GetImageStream();
                        await imageStream.CopyToAsync(stream);
                    }

                    if (close)
                    {
                        producer.CloseImage();
                    }
                }
            }

            return _imageProducers.Select(x => x.GetPageNumber());
        }
    }
}
