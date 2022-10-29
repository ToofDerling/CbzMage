using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System.IO.Pipes;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPipedImageStream 
    {
        private static readonly byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        private readonly IPipedImageDataHandler _imageDatahandler;

        private AnonymousPipeServerStream _pipe;

        public GhostscriptPipedImageStream(IPipedImageDataHandler imageDatahandler)
        {
            _imageDatahandler = imageDatahandler;

            _pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable, Settings.PipeBufferSize);

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
            var currentBuffer = new ManagedBuffer();
            var firstImage = true;

            var offset = 0;
            int readCount;

            while ((readCount = currentBuffer.ReadFrom(_pipe)) > 0)
            {

#if DEBUG
                StatsCount.AddPipeRead(readCount);
#endif

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
