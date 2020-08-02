using System.Threading.Tasks;

namespace Rotvel.PdfConverter
{
    public class CompressCbzTask : Task
    {
        public Pdf Pdf { get; private set; }

        public CompressCbzTask(Pdf pdf) : base(() => pdf.CompressPages())
        {
            Pdf = pdf;
        }
    }
}