using CbzMage.Shared.Helpers;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

namespace CoreComicsConverter.CbzCbrFlow
{
    public class CbzFlow
    {
        public void ExtractCbz(CbzComic cbzComic)
        {
            var sevenZip = new SevenZipMachine();

            sevenZip.Extracted += (s, e) => ProgressReporter.ShowMessage($"Extracted {e.Progress}");

            var extractedFiles = sevenZip.ExtractComic(cbzComic);

            ProgressReporter.EndMessages();

            cbzComic.Files = extractedFiles;
            cbzComic.PageCount = cbzComic.ImageCount = extractedFiles.Length;
        }

        public void CreatePdf(CbzComic cbzComic)
        {
            var pdfCreator = new PdfCreator();

            var progressReporter = new ProgressReporter(cbzComic.PageCount);
            pdfCreator.PageCreated += (s, e) => progressReporter.ShowProgress($"{e.Page.Name} created");

            pdfCreator.CreatePdf(cbzComic);

            progressReporter.EndProgress();
        }
    }
}
