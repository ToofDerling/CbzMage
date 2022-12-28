using CbzMage.Shared.Buffers;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive? _compressor;

        private readonly List<(string page, ArrayPoolBufferWriter<byte> image)> _imageList;

        private readonly ProgressReporter _progressReporter;

        private readonly string? _coverFile;

        public ImageCompressorJob(ZipArchive? compressor, List<(string, ArrayPoolBufferWriter<byte>)> imageList,
            ProgressReporter progressReporter, string? coverFile = null)
        {
            _compressor = compressor;

            _imageList = imageList;

            _progressReporter = progressReporter;

            _coverFile = coverFile;
        }

        public IEnumerable<string> Execute()
        {
            var firstPage = true;

            foreach (var (page, bufferWriter) in _imageList)
            {
                var imageData = bufferWriter.WrittenSpan;

                if (firstPage)
                {
                    if (_coverFile != null)
                    {
                        using var coverStream = new FileStream(_coverFile, FileMode.Create);
                        coverStream.Write(imageData);

                        if (_compressor == null)
                        {
                            _progressReporter.ShowProgress($"Saved {page}");
                        }
                    }
                    firstPage = false;
                }

                if (_compressor != null)
                {
                    var entry = _compressor.CreateEntry(page);

                    using var cbzStream = entry.Open();
                    cbzStream.Write(imageData);

                    _progressReporter.ShowProgress($"Converted {page}");
                }

                bufferWriter.Close();
            }

            return _imageList.Select(x => x.page);
        }
    }
}
