using CoreComicsConverter.DirectoryFlow;
using CoreComicsConverter.Extensions;
using CoreComicsConverter.Helpers;
using ImageMagick;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System;
using System.IO;
using System.Linq;

namespace CoreComicsConverter
{
    public class ArchiveConverter
    {
        public void ConvertToPdf(DirectoryComic comic)
        {
            //File.Delete(comic.Path);

            using var pdfWriter = new PdfWriter(comic.Path + ".pdf");
            using var pdfDocument = new PdfDocument(pdfWriter);
            using var document = new Document(pdfDocument);

            var documentInfo = pdfDocument.GetDocumentInfo();
            documentInfo.SetAuthor("CoreComicsConverter"); //TODO:
            documentInfo.AddCreationDate();

            document.SetMargins(0, 0, 0, 0);

            var images = Directory.EnumerateFiles(comic.Path).OrderBy(f => f.ToString()).AsList();

            comic.PageCount = images.Count;
            var progressReporter = new ProgressReporter(comic.PageCount);

            foreach (var imagePath in images)
            {
                var resized = false;
                using (var magickImage = new MagickImage())
                {
                    magickImage.Ping(imagePath);

                    if (magickImage.Width > 1073)
                    {
                        magickImage.Read(imagePath);
                        magickImage.Resize(2146, 1650);
                        magickImage.Write(imagePath);

                        resized = true;
                    }
                };

                var imageData = ImageDataFactory.Create(imagePath);
                var pageSize = new PageSize(imageData.GetWidth(), imageData.GetHeight());

                pdfDocument.AddNewPage(pageSize);
                if (resized)
                {
                    pdfDocument.AddNewPage(new PageSize(0, imageData.GetHeight()));
                }

                var image = new Image(imageData);
                document.Add(image);

                var imageName = System.IO.Path.GetFileName(imagePath);
                progressReporter.ShowProgress(imageName);
            }
        }
    }
}
