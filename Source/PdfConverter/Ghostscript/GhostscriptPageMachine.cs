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

        private string[] GetSwitches(string pdfFile, string pageList, int dpi, string output)
        {
            var switches = new[]
            {
                //"-empty", wut?
                // "-dQUIET", handled by -q
                // "-dNOSAFER", not needed for gs 10.0
                "-dTextAlphaBits=1", // Turn off subsample antialiasing
                "-dGraphicsAlphaBits=1", // Turn off subsample antialiasing
                "-dUseCropBox",
                //"-dBATCH", handled by -o
                //"-dNOPAUSE", handled by -o
                "-dNOPROMPT",
                "-sDEVICE=png16m",
                //"-sDEVICE=png16malpha", causes inverted colors on editorial pages in many books
                //$"-dMaxBitmap={BufferSize}", this is for X only
                //$"-dNumRenderingThreads={Environment.ProcessorCount}",
                $"-sPageList={pageList}",
                $"-r{dpi}",
                $"-o{output}",
                "-q", // Don't write to stdout (and set -dQUIET)
                $"-f{pdfFile}", // -f skips a few filename checks
                //pdfFile
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

        public void SavePageList(Pdf pdf, List<int> pageNumbers, int dpi, string outputId)
        {
            var pageList = CreatePageList(pageNumbers);

            var switches = GetSwitches(pdf.Path, pageList, dpi, @$"{outputId}/%d.png");

            _processor.StartProcessing(switches, null);
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
