using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly ZipArchive _compressor;

        private readonly List<(string page, MagickImage image)> _imageList;

        private readonly ProgressReporter _progressReporter;

        public ImageCompressorJob(ZipArchive compressor, List<(string, MagickImage)> imageList,
            ProgressReporter progressReporter)
        {
            _compressor = compressor;

            _imageList = imageList;

            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {

#if DEBUG
            var stopwatch = new Stopwatch();
#endif

            foreach (var (page, image) in _imageList)
            {

#if DEBUG
                stopwatch.Restart();
#endif
                
                int written = ImageHelper.CompressAndCloseImage(image, _compressor, page);

#if DEBUG 
                stopwatch.Stop();
                StatsCount.AddMagickWrite((int)stopwatch.ElapsedMilliseconds, written);
#endif

                _progressReporter.ShowProgress($"Converted {page}");
            }

            return _imageList.Select(x => x.page);
        }
    }
}
