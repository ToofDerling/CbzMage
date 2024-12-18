using CbzMage.Shared.Buffers;
using ImageMagick;
using PdfConverter.Helpers;
using PdfConverter.ImageProducer;
using System.Diagnostics;

namespace PdfConverter.ImageData
{
    public class ImageConverter
    {
        private AbstractImageProducer _imageProducer;

        public ImageConverter(AbstractImageProducer pageInfo)
        {
            _imageProducer = pageInfo;
        }

        public async Task ConvertAsync()
        {
            // Don't touch saved jpgs
            if (_imageProducer is PopplerSaveImageProducer saveImageProducer && saveImageProducer.GetImageExt() == PdfImageExt.Jpg)
            {
                return;
            }

#if DEBUG 
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var resized = false;
            var pngSize = 0;

            if (_imageProducer is PopplerSaveImageProducer savedImageProducer)
            {
                savedImageProducer.ConvertedImageData = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

                savedImageProducer.WaitForExit();

                // TODO: verify if recompressing saved pngs still makes sense.
                using (var image = new MagickImage())
                {

                    using (var stream = savedImageProducer.GetImageStream())
                    {
                        await image.ReadAsync(stream);
                    }

                    //var file = savedImage.GetSavedImagePath();
                    //image.Format = MagickFormat.Png;
                    //image.Quality = 100;
                    //file = Path.ChangeExtension(file, ".png");
                    //file = file.Replace("000", "001");
                    //await image.WriteAsync(file);

                    image.Format = MagickFormat.Png;
                    image.Quality = 100;
                    image.Write(savedImageProducer.ConvertedImageData);

                    savedImageProducer.SetImageExt(PdfImageExt.Png);

                }

                //File.Delete(savedImage.GetSavedImagePath());
            }
            else if (_imageProducer is PopplerRenderImageProducer renderedImageProducer)
            {
                //pngSize = renderedImage.ImageData.WrittenCount;

                renderedImageProducer.ConvertedImageData = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

                using var stream = renderedImageProducer.GetImageStream();

                int readCount = 0;

                while (true)
                {
                    var memory = renderedImageProducer.ConvertedImageData.GetMemory(Settings.WriteBufferSize);

                    readCount = await stream.ReadAsync(memory);
                    if (readCount == 0)
                    {
                        break;
                    }
                    renderedImageProducer.ConvertedImageData.Advance(readCount);
                }

                renderedImageProducer.WaitForExit();

                using var image = new MagickImage(renderedImageProducer.ConvertedImageData.WrittenSpan);

                // Produce baseline jpgs with no subsampling.
                image.Format = MagickFormat.Jpg;
                image.Quality = Settings.JpgQuality;

                var resizeHeight = renderedImageProducer.GetResizeHeight();

                if (resizeHeight.HasValue && image.Height > resizeHeight.Value)
                {
                    resized = true;

                    image.Resize(new MagickGeometry
                    {
                        Greater = true,
                        Less = false,
                        Height = resizeHeight.Value
                    });
                }

                // Reuse the png buffer for the jpg
                renderedImageProducer.ConvertedImageData.Reset();
                
                image.Write(renderedImageProducer.ConvertedImageData);

                renderedImageProducer.SetImageExt(PdfImageExt.Jpg);
            }

#if DEBUG
            stopwatch.Stop();
            StatsCount.AddImageConversion((int)stopwatch.ElapsedMilliseconds, resized, pngSize);
#endif

        }
    }
}
