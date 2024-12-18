using PdfConverter.Helpers;
using CbzMage.Shared.Buffers;
using System.Diagnostics;

namespace PdfConverter.ImageData
{
    public class PngStreamReader
    {
        private static readonly byte[] _pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        private readonly IImageDataHandler _imageDatahandler;

        private readonly Stream _stream;

        public PngStreamReader(Stream stream, IImageDataHandler imageDatahandler)
        {
            _stream = stream;
            _imageDatahandler = imageDatahandler;
        }

        public void StartReadingImages()
        {
            Task.Factory.StartNew(ReadGhostscriptOutput, TaskCreationOptions.LongRunning);
        }

        private void ReadGhostscriptOutput()
        {
            try
            {
                var currentBufferWriter = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);
                var firstImage = true;

                var offset = 0;
                int readCount;

                var pngHeaderSpan = _pngHeader.AsSpan();

                var imageCount = 0; // Only used for debugging

                while (true)
                {
                    var span = currentBufferWriter.GetSpan(Settings.WriteBufferSize);

                    readCount = _stream.Read(span);
                    if (readCount == 0)
                    {
                        break;
                    }

                    LogRead(readCount);
                    currentBufferWriter.Advance(readCount);

                    var readCountSpan = currentBufferWriter.WrittenSpan[^readCount..];
                    var pngIdx = readCountSpan.IndexOf(pngHeaderSpan);

                    if (pngIdx != -1)
                    {
                        imageCount++;

                        // Buffer contains a full image plus the first read of the next
                        if (!firstImage)
                        {
                            // Create next buffer and copy next image bytes into it
                            var nextBufferWriter = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

                            var nextDataStart = offset + pngIdx;
                            var nextData = currentBufferWriter.WrittenSpan[nextDataStart..];

                            var nextSpan = nextBufferWriter.GetSpan(nextData.Length);
                            nextData.CopyTo(nextSpan);

                            Debug.Assert(nextSpan.StartsWith(pngHeaderSpan), $"image-{imageCount}: nextSpan does not start with png header");

                            nextBufferWriter.Advance(nextData.Length); // Next buffer has first part of next image 
                            currentBufferWriter.Withdraw(nextData.Length); // Current buffer has current image

                            Debug.Assert(currentBufferWriter.WrittenSpan.StartsWith(pngHeaderSpan), $"image-{imageCount}: currentBufferWriter.WrittenSpan does not start with png header");
                            
                            _imageDatahandler.HandleRenderedImageData(currentBufferWriter);

                            currentBufferWriter = nextBufferWriter;

                            offset = readCount - pngIdx; // Adjust offset for bytes in next buffer
                        }
                        else
                        {
                            // Keep reading if it's the first image
                            firstImage = false;
                            offset += readCount;
                        }
                    }
                    else
                    {
                        offset += readCount;
                    }
                }

                if (offset > 0)
                {
                    _imageDatahandler.HandleRenderedImageData(currentBufferWriter);
                }

                // Signal we're done.
                _imageDatahandler.HandleImageData(null!);
            }
            finally
            {
                // Relying on the IDisposable pattern can cause a nullpointerexception
                // because the stream is ripped out right under the last read.
                _stream.Dispose();
            }
        }

        private static void LogRead(int readCount)
        {
#if DEBUG
            StatsCount.AddStreamRead(readCount);
#endif
        }
    }
}
