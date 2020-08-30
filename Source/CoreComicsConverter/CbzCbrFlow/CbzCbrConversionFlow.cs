using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

namespace CoreComicsConverter.CbzCbrFlow
{
    public class CbzCbrConversionFlow
    {
        private DirectoryConversionFlow _directoryConversionFlow = new DirectoryConversionFlow();

        public void ExtractCbz(DirectoryComic cbzComic)
        {
            var sevenZip = new SevenZipMachine();

            var progressReporter = new ProgressReporter(0);
            sevenZip.Extracted += (s, e) => progressReporter.ShowMessage($"Extracted {e.Progress}");

            var extractedFiles = sevenZip.ExtractComic(cbzComic);

            cbzComic.Files = extractedFiles;
            cbzComic.PageCount = cbzComic.ImageCount = extractedFiles.Length;

            Console.WriteLine();
        }

        public List<ComicPage> ParseImagesSetPageCount(DirectoryComic cbzComic)
        {
            var pageSizes = _directoryConversionFlow.ParseImagesSetPageCount(cbzComic);
            return pageSizes;
        }

        public void CreatePdf(CbzComic cbzComic)
        {
            var pdfCreator = new PdfCreator();

            var progressReporter = new ProgressReporter(cbzComic.PageCount);
            pdfCreator.PageCreated += (s, e) => progressReporter.ShowProgress($"{e.Page.Name} created");

            pdfCreator.CreatePdf(cbzComic);
        }
    }
}
