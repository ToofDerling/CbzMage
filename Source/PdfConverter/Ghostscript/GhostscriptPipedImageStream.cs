using ImageMagick;
using PdfConverter.Helpers;
using System.Buffers;
using System.IO.Pipes;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPipedImageStream 
    {
        private static readonly byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        private const int pipeBufferSize = 65536;
        private const int imageDataSize = 5242880;

        private readonly IPipedImageDataHandler _imageDatahandler;

        private AnonymousPipeServerStream _pipe;

        public GhostscriptPipedImageStream(IPipedImageDataHandler imageDatahandler)
        {
            _imageDatahandler = imageDatahandler;

            _pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable, pipeBufferSize);

            var thread = new Thread(new ThreadStart(ReadGhostscriptPipedOutput));
            thread.Start();
        }

        public string GetOutputPipeHandle()
        {
            //Pipe handle format: %handle%hexvalue
            var outputPipeHandle = $"%handle%{int.Parse(_pipe.GetClientHandleAsString()):X2}";
            return outputPipeHandle;
        }

        private void ReadGhostscriptPipedOutput()
        {
            var writer = new ArrayBufferWriter<byte>(imageDataSize);

            var firstImage = true;
            var pngSpan = pngHeader.AsSpan();

            while (true)
            {
                var data = writer.GetSpan(pipeBufferSize);

                var readCount = _pipe.Read(data);
                if (readCount == 0)
                {
                    break;
                }

                StatsCount.AddRead(readCount);

                //Image header is found at the start position of a read
                if (readCount > pngSpan.Length && data.StartsWith(pngSpan))
                {
                    //Buffer contains a full image plus the first read of the next
                    if (!firstImage)
                    {
                        // Save the first bit of the next image
                        var nextImageData = new byte[readCount].AsSpan();
                        data[..readCount].CopyTo(nextImageData);

                        StatsCount.AddPng(writer.WrittenCount);

                        var image = new MagickImage(writer.WrittenSpan);
                        _imageDatahandler.HandleImageData(image);
                        
                        // Prepare writer for reuse
                        writer.Clear();
                        var startOfNextImage = writer.GetSpan(pipeBufferSize);

                        // Write the first bit of the next image
                        nextImageData.CopyTo(startOfNextImage);
                        writer.Advance(readCount);
                    }
                    else
                    {
                        // Keep reading if it's the first image
                        firstImage = false;
                        writer.Advance(readCount);
                    }
                }
                else
                {
                    writer.Advance(readCount);
                }
            }

            var lastImage = new MagickImage(writer.WrittenSpan);
            _imageDatahandler.HandleImageData(lastImage);

            // Signal we're done reading.
            _imageDatahandler.HandleImageData(null);

            // Close clienthandle and pipe safely when we're done reading.
            // Relying on the IDisposable pattern can cause a nullpointerexception
            // because the pipe is ripped out right under the last read.

            _pipe.ClientSafePipeHandle.SetHandleAsInvalid();
            _pipe.ClientSafePipeHandle.Dispose();
            
            _pipe.Dispose();
            _pipe = null;

            // Original Ghostscript.NET comment on why SetHandleAsInvalid is
            // necessary:
            // for some reason at this point the handle is invalid for real.
            // DisposeLocalCopyOfClientHandle should be called instead, but it 
            // throws an exception saying that the handle is invalid pointing to 
            // CloseHandle method in the dissasembled code.
            // this is a workaround, if we don't set the handle as invalid, when
            // garbage collector tries to dispose this handle, exception is thrown
        }
    }
}
