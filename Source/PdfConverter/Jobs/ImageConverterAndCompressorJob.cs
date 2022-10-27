using CbzMage.Shared.Helpers;
using CbzMage.Shared.Jobs;
using ImageMagick;
using PdfConverter.Helpers;
using System.Diagnostics;
using System.IO.Compression;

namespace PdfConverter.Jobs
{
    public class ImageConverterAndCompressorJob : IJob<IEnumerable<string>>
    {
        private readonly int? _resizeHeight;

        private readonly List<(string page, string imagePath)> _imageList;

        private readonly ProgressReporter _progressReporter;

        private readonly ZipArchive _compressor;

        public ImageConverterAndCompressorJob(List<(string, string)> imageList, 
            int? resizeHeight,
            ZipArchive compressor, 
            ProgressReporter progressReporter)
        {
            _resizeHeight = resizeHeight;   

            _compressor = compressor;
            _imageList = imageList;
            _progressReporter = progressReporter;
        }

        public IEnumerable<string> Execute()
        {

#if DEBUG
            var stopwatch = new Stopwatch();
#endif

            foreach (var (page, imagePath) in _imageList)
            {

#if DEBUG 
                stopwatch.Restart();
#endif

                MagickImage image = null;

                var retries = 0;
                for (; retries < 100; retries++)
                {
                    // Loading the first image can fail when Ghostscript is not finished
                    // saving it. There's nothing to do but wait for it to finish.
                    try
                    {
                        image = new MagickImage(imagePath);
                        if (image != null)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }

                // Fail hard if we didn't get an image after 10 seconds.
                if (image == null)
                {
                    throw new IOException($"Timed out loading {page} ({imagePath})");
                }

                var resized = ImageHelper.ConvertJpg(image, _resizeHeight);

                var entry = _compressor.CreateEntry(page, Settings.CompressionLevel);
                using var archiveStream = entry.Open();

                image.Write(archiveStream);
                image.Dispose();

#if DEBUG 
                stopwatch.Stop();
                StatsCount.AddMagickReadWrite((int)stopwatch.ElapsedMilliseconds, retries, resized);
#endif

                _progressReporter.ShowProgress($"Converted {page}");
            }
            
            return _imageList.Select(x => x.page);
        }
    }
}
