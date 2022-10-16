//
// GhostscriptPipedOutput.cs
// This file is part of Ghostscript.NET library
//
// Author: Josip Habjan (habjan@gmail.com, http://www.linkedin.com/in/habjan) 
// Copyright (c) 2013-2016 by Josip Habjan. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PdfConverter.Helpers;
using PdfConverter.ManagedBuffers;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace PdfConverter.Ghostscript
{
    //Adapted from Ghostscript.Net GhostscriptPipedOutput.cs
    public class GhostscriptPipedImageStream : IDisposable
    {
        private bool _disposed = false;
        private AnonymousPipeServerStream _pipe;
        private Thread _thread = null;

        private readonly byte[] _imageHeader;

        //StatsCount haven't reported reads above 500000 bytes so this should be enough
        private const int PipeBufferSize = 1000000;

        private readonly IPipedImageDataHandler _imageDatahandler;

        public GhostscriptPipedImageStream(byte[] imageHeader, IPipedImageDataHandler imageDatahandler)
        {
            _imageHeader = imageHeader;
            _imageDatahandler = imageDatahandler;

            _pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable, PipeBufferSize);

            _thread = new Thread(new ThreadStart(ReadGhostscriptPipedOutput));
            _thread.Start();
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
                StatsCount.AddRead(readCount);

                //Image header is found at the start position of a read
                if (currentBuffer.StartsWith(offset, readCount, _imageHeader))
                {
                    //Buffer contains a full image plus the first read of the next
                    if (!firstImage)
                    {
                        //Create next buffer and copy next image bytes into it
                        var nextBuffer = new ManagedBuffer(currentBuffer, offset, readCount);

                        _imageDatahandler.HandleImageData(currentBuffer);
                        StatsCount.AddPng(offset);

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
                StatsCount.AddPng(offset);
            }

            _imageDatahandler.HandleImageData(null);
        }

        #region IDisposable 

        ~GhostscriptPipedImageStream()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_pipe != null)
                    {
                        // _pipe.DisposeLocalCopyOfClientHandle();

                        // for some reason at this point the handle is invalid for real.
                        // DisposeLocalCopyOfClientHandle should be called instead, but it 
                        // throws an exception saying that the handle is invalid pointing to 
                        // CloseHandle method in the dissasembled code.
                        // this is a workaround, if we don't set the handle as invalid, when
                        // garbage collector tries to dispose this handle, exception is thrown
                        _pipe.ClientSafePipeHandle.SetHandleAsInvalid();

                        _pipe.Dispose();
                        _pipe = null;
                    }

                    if (_thread != null)
                    {
                        // check if the thread is still running
                        if (_thread.ThreadState == ThreadState.Running)
                        {
                            // abort the thread
                            _thread.Abort();
                        }

                        _thread = null;
                    }
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
