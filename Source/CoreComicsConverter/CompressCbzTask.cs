using CoreComicsConverter.PdfFlow;
using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class CompressCbzTask : Task
    {
        public PdfComic PdfComic { get; private set; }

        public CompressCbzTask(PdfComic pdfComic) : base(() => pdfComic.CompressPages())
        {
            PdfComic = pdfComic;
        }
    }
}