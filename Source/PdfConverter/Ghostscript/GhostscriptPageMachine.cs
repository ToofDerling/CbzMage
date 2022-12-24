using CbzMage.Shared.Helpers;
using System.Text;

namespace PdfConverter.Ghostscript
{
    public class GhostscriptPageMachine
    {
        private static string[] GetSwitches(string pdfFile, string pageList, int dpi, string output)
        {
            var numRenderingThreads = string.Empty;
            if (Settings.GhostscriptReaderThreads == 1)
            {
                numRenderingThreads = $"-dNumRenderingThreads={Environment.ProcessorCount}";
            }

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
                numRenderingThreads,            
                $"-sPageList={pageList}",
                $"-r{dpi}",
                $"-o -", // write image output to stdout
                "-q", // Don't write text output to stdout (and set -dQUIET)
                $"-f\"{pdfFile}\"", // -f skips a few filename checks
                //pdfFile
            };

            return switches;
        }

        private static string CreatePageList(List<int> pageNumbers)
        {
            var sb = new StringBuilder();

            pageNumbers.ForEach(p => sb.Append(p).Append(','));
            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public ProcessRunner StartReadingPages(Pdf pdf, List<int> pageNumbers, int dpi, IPipedImageDataHandler imageDataHandler)
        {
            var pageList = CreatePageList(pageNumbers);

            var gsPath = Settings.GhostscriptPath;
            var gsSwitches = GetSwitches(pdf.Path, pageList, dpi, string.Empty);

            var parameters = string.Join(' ', gsSwitches);

            var gsRunner = new ProcessRunner(gsPath, parameters);
            gsRunner.Run();

            var stream = gsRunner.GetOutputStream();

            var gsPipedOutput = new GhostscriptPipedImageStream(stream, imageDataHandler);
            gsPipedOutput.StartReadingImages();

            return gsRunner;
        }
    }
}
