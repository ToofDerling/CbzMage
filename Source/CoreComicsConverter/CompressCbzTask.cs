using System.Threading.Tasks;

namespace CoreComicsConverter
{
    public class CompressCbzTask : Task
    {
        public PdfComic Pdf { get; private set; }

        public CompressCbzTask(PdfComic pdf) : base(() => pdf.CompressPages())
        {
            Pdf = pdf;
        }
    }
}