using CoreComicsConverter.Model;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using PdfConverter.Events;
using System;
using System.IO;

namespace PdfConverter.CbzFlow
{
    public class PdfCreator
    {
        public event EventHandler<PageEventArgs> PageCreated;

        public void CreatePdf(CbzComic comic)
        {
            File.Delete(comic.OutputFile);

            using var pdfWriter = new PdfWriter(comic.OutputFile);
            using var pdfDocument = new PdfDocument(pdfWriter);
            using var document = new Document(pdfDocument);

            var documentInfo = pdfDocument.GetDocumentInfo();
            documentInfo.SetAuthor("CoreComicsConverter"); //TODO:
            documentInfo.AddCreationDate();

            document.SetMargins(0, 0, 0, 0);

            foreach (var imagePath in comic.Files)
            {
                var imageData = ImageDataFactory.Create(imagePath);
                var pageSize = new PageSize(imageData.GetWidth(), imageData.GetHeight());

                pdfDocument.AddNewPage(pageSize);

                var image = new Image(imageData);
                document.Add(image);

                var imageName = System.IO.Path.GetFileName(imagePath);
                PageCreated?.Invoke(this, new PageEventArgs(new ComicPage() { Name = imageName }));
            }
        }
    }
}
