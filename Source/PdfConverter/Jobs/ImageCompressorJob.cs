using CbzMage.Shared.Buffers;
using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly List<(string page, ArrayPoolBufferWriter<byte> image)> _imageList;

        private readonly ProgressReporter _progressReporter;

        public ImageCompressorJob(ZipArchive compressor, List<(string, ArrayPoolBufferWriter<byte>)> imageList,
            ProgressReporter progressReporter)
        {
            _compressor = compressor;

            _imageList = imageList;

            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {
            foreach (var (page, bufferWriter) in _imageList)
            {
                var entry = _compressor.CreateEntry(page);
                using var cbzStream = entry.Open();

                cbzStream.Write(bufferWriter.WrittenSpan);

                bufferWriter.Close();

                _progressReporter.ShowProgress($"Converted {page}");
            }

            return _imageList.Select(x => x.page);
        }
    }
}
