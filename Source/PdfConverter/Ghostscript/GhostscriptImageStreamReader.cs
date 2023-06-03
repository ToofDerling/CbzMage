using PdfConverter.Helpers;
using CbzMage.Shared.Buffers;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptImageStreamReader
    {
        private static readonly byte[] _pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        private readonly IImageDataHandler _imageDatahandler;

        private readonly Stream _stream;

        public GhostscriptImageStreamReader(Stream stream, IImageDataHandler imageDatahandler)
        {
            _stream = stream;
            _imageDatahandler = imageDatahandler;
        }

        public void StartReadingImages()
        {
            var thread = new Thread(new ThreadStart(ReadGhostscriptOutput));
            thread.Start();
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

                    //Image header is found at the start position of a read
                    if (span.StartsWith(pngHeaderSpan))
                    {
                        //Buffer contains a full image plus the first read of the next
                        if (!firstImage)
                        {
                            //Create next buffer and copy next image bytes into it
                            var nextBufferWriter = new ArrayPoolBufferWriter<byte>(Settings.ImageBufferSize);

                            var data = currentBufferWriter.WrittenSpan.Slice(offset, readCount);
                            var nextSpan = nextBufferWriter.GetSpan(data.Length);
                            data.CopyTo(nextSpan);

                            nextBufferWriter.Advance(data.Length); //next buffer has first part of next image 
                            currentBufferWriter.Withdraw(data.Length); //current buffer has current image

                            _imageDatahandler.HandleParsedImageData(currentBufferWriter);

                            currentBufferWriter = nextBufferWriter;

                            offset = readCount; //We already have readCount bytes in next buffer
                        }
                        else
                        {
                            //Keep reading if it's the first image
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
                    _imageDatahandler.HandleParsedImageData(currentBufferWriter);
                }

                // Signal we're done.
                _imageDatahandler.HandleParsedImageData(null!);
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
