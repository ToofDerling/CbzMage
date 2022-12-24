using PdfConverter.Helpers;
using CbzMage.Shared.ManagedBuffers;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPipedImageStream
    {
        private static readonly byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        private readonly IPipedImageDataHandler _imageDatahandler;

        private readonly Stream _pipe;

        public GhostscriptPipedImageStream(Stream stream, IPipedImageDataHandler imageDatahandler)
        {
            _pipe = stream;
            _imageDatahandler = imageDatahandler;

        }

        public void StartReadingImages()
        {
            var thread = new Thread(new ThreadStart(ReadGhostscriptPipedOutput));
            thread.Start();
        }

        private void ReadGhostscriptPipedOutput()
        {
            try
            {
                var currentBuffer = new ManagedBuffer();
                var firstImage = true;

                var offset = 0;
                int readCount;

                while ((readCount = currentBuffer.ReadFrom(_pipe)) > 0)
                {
                    LogPipeRead(readCount);

                    //Image header is found at the start position of a read
                    if (currentBuffer.StartsWith(offset, readCount, pngHeader))
                    {
                        //Buffer contains a full image plus the first read of the next
                        if (!firstImage)
                        {
                            //Create next buffer and copy next image bytes into it
                            var nextBuffer = new ManagedBuffer(currentBuffer, offset, readCount);
                            _imageDatahandler.HandleImageData(currentBuffer);

                            currentBuffer = nextBuffer;
                            offset = readCount; //We already have readCount bytes in new buffer
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
                    _imageDatahandler.HandleImageData(currentBuffer);
                }

                // Signal we're done.
                _imageDatahandler.HandleImageData(null!);
            }
            finally
            {
                // Relying on the IDisposable pattern can cause a nullpointerexception
                // because the pipe is ripped out right under the last read.
                _pipe.Dispose();
            }
        }

        private static void LogPipeRead(int readCount)
        {
#if DEBUG
            StatsCount.AddPipeRead(readCount);
#endif
        }
    }
}
