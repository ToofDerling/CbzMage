using CbzMage.Shared.Extensions;
using CbzMage.Shared.Helpers;
using PdfConverter.PageInfo;

namespace PdfConverter.PageMachines
{
    public class PopplerSavePageMachine
    {
        private static string[] GetSwitches(string pdfFile, int pageNumber, string imageRoot)
        {
            var switches = new[]
            {
                $"-f {pageNumber}", // first page to print
                $"-l {pageNumber}", // last page to print
                "-all", // equivalent to -png -tiff -j -jp2 -jbig2 -ccitt
                "-q", // don't print any messages or errors
                //"-j", // write JPEG images as JPEG files
                //"-p", // include page numbers in output file names
                $"\"{pdfFile}\"",
                $"\"{imageRoot}\""
            };

            return switches;
        }

        private string _savedPagePath;

        public string GetSavedPagePath() => _savedPagePath;

        public ProcessRunner SavePage(Pdf pdf, AbstractPdfPageInfo pdfPageInfo, string saveDirectory)
        {
            var imageRoot = Path.Combine(saveDirectory, pdfPageInfo.PageNumber.ToPageNumberString());

            var pdfImagesPath = Settings.PdfImagesPath;
            var switches = GetSwitches(pdf.PdfPath, pdfPageInfo.PageNumber, imageRoot);

            var parameters = string.Join(' ', switches);
            var pdfImagesRunner = new ProcessRunner(pdfImagesPath, parameters);

            pdfImagesRunner.Run();

            _savedPagePath = $"{imageRoot}-000.{pdfPageInfo.LargestImageExt}";

            return pdfImagesRunner;
        }
    }
}
