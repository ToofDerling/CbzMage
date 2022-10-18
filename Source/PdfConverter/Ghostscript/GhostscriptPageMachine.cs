using Ghostscript.NET.Processor;
using System.Text;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPageMachine : IDisposable
    {
        private readonly GhostscriptProcessor _processor;

        public GhostscriptPageMachine(GhostscriptProcessor processor)
        {
            _processor = processor;
        }

        private string[] GetSwitches(string pdfFile, string pageList, int dpi, string outputPipeHandle)
        {
            var switches = new[]
            {
                "-empty",
                "-dQUIET",
               // "-dNOSAFER", Not needed for gs 10.0
                "-dTextAlphaBits=4",
                "-dGraphicsAlphaBits=4",
                "-dUseCropBox",
                "-dBATCH",
                "-dNOPAUSE",
                "-dNOPROMPT",
                "-sDEVICE=png16malpha",
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
            var gsPipedOutput = new GhostscriptPipedImageStream(imageDataHandler);

            var pageList = CreatePageList(pageNumbers);

            var outputPipeHandle = gsPipedOutput.GetOutputPipeHandle();
            var switches = GetSwitches(pdf.Path, pageList, dpi, outputPipeHandle);

            _processor.StartProcessing(switches, null);
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
