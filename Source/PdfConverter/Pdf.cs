namespace PdfConverter
{
    public sealed class Pdf
    {
        public Pdf(string path)
        {
            PdfPath = path;

            SaveDirectory = Path.ChangeExtension(PdfPath, null);
#if !DEBUG
            SaveDirectory = $"{SaveDirectory}.{Path.GetRandomFileName()}";
#endif
        }

        public string PdfPath { get; private set; }

        public string SaveDirectory { get; private set; }

        public int PageCount { get; set; }

        public int ImageCount { get; set; }

        public static List<Pdf> List(params string[] paths) => new(paths.Select(x => new Pdf(x)));
    }
}
