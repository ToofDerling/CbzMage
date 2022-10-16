using Ghostscript.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPageMachine : IDisposable
    {
        private readonly GhostscriptProcessor _processor;

        private static readonly byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG "\x89PNG\x0D\0xA\0x1A\0x0A"

        public GhostscriptPageMachine(GhostscriptProcessor processor)
        {
            _processor = processor;
        }

        private string[] GetSwitches(string pdfFile, string pageList, int dpi, string outputPipeHandle)
        {
            //TODO: play with settings from https://stackoverflow.com/questions/4548919/any-tips-for-speeding-up-ghostscript

            var switches = new[]
            {
                "-empty",
                "-dQUIET",
                "-dNOSAFER", // Required for gs 9.50
                "-dBATCH",
                "-dNOPAUSE",
                "-dNOPROMPT",
                "-sDEVICE=png16m",
                //$"-dMaxBitmap={BufferSize}", This is for X only
                //$"-dNumRenderingThreads={Environment.ProcessorCount}",
                $"-sPageList={pageList}",
                $"-r{dpi}",
                "-o" + outputPipeHandle,
                "-q",
                "-f",
                pdfFile
            };

            return switches;
        }

        private string CreatePageList(List<int> pageNumbers)
        {
            var sb = new StringBuilder();

            pageNumbers.ForEach(p => sb.Append(p).Append(','));
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public void ReadPageList(Pdf pdf, List<int> pageNumbers, int dpi, IPipedImageDataHandler imageDataHandler)
        {
            using (var gsPipedOutput = new GhostscriptPipedImageStream(pngHeader, imageDataHandler))
            {
                var pdfFile = pdf.Path;
                var pageList = CreatePageList(pageNumbers);

                var outputPipeHandle = gsPipedOutput.GetOutputPipeHandle();
                var switches = GetSwitches(pdf.Path, pageList, dpi, outputPipeHandle);

                _processor.StartProcessing(switches, null);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    _processor.Dispose();
                }

                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GhostScriptPageMachine() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}