using CbzMage.Shared.Helpers;
using PdfConverter.PageInfo;

namespace PdfConverter.PageMachines
{
    public class PopplerRenderPageMachine
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

        public ProcessRunner RenderPage(Pdf pdf, PdfPageInfoRenderImage pageInfoRenderImage)
        {
            return RenderPage(pdf, new List<int> { pageInfoRenderImage.PageNumber }, pageInfoRenderImage.Dpi);
        }

        public ProcessRunner RenderPage(Pdf pdf, List<int> pageNumbers, int dpi)
        {
            var pdfToPngPath = Settings.PdfToPngPath;
            var switches = GetSwitches(pdf.PdfPath, pageNumbers.First(), pageNumbers.Last(), dpi);

            var parameters = string.Join(' ', switches);
            var pdfToPngRunner = new ProcessRunner(pdfToPngPath, parameters);

            pdfToPngRunner.Run();

            return pdfToPngRunner;
        }
    }
}
