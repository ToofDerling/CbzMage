using CbzMage.Shared.Helpers;
using PdfConverter.ImageData;

namespace PdfConverter.PageMachines
{
    public class PopplerPageMachine
    {
        private static string[] GetSwitches(string pdfFile, int firstPage, int lastPage, int dpi)
        {
            var switches = new[]
            {
                $"-f {firstPage}", // first page to print
                $"-l {lastPage}", // last page to print
                $"-r {dpi}", // resolution, in DPI (default is 150)
                "-png", //  generate a PNG file
                "-q", // don't print any messages or errors
                $"\"{pdfFile}\""
            };

            return switches;
        }

        public ProcessRunner StartReadingPages(Pdf pdf, List<int> pageNumbers, int dpi, IImageDataHandler imageDataHandler)
        {
            var pdfToPngPath = Settings.PdfToPngPath;
            var switches = GetSwitches(pdf.Path, pageNumbers.First(), pageNumbers.Last(), dpi);

            var parameters = string.Join(' ', switches);
            var pdfToPngRunner = new ProcessRunner(pdfToPngPath, parameters);

            pdfToPngRunner.Run();
            var stream = pdfToPngRunner.GetOutputStream();

            var pngOutputReader = new PngStreamReader(stream, imageDataHandler);
            pngOutputReader.StartReadingImages();

            return pdfToPngRunner;
        }
    }
}
