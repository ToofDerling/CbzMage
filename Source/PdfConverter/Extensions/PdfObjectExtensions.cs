using iTextSharp.text.pdf;

namespace PdfConverter.Extensions
{
    public static class PdfObjectExtensions
    {
        public static int ToInt(this PdfObject pdfObject)
        {
            return ((PdfNumber)pdfObject).IntValue;
        }
    }
}
