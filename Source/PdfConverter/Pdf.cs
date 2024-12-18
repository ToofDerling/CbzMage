namespace PdfConverter
{
    public sealed class Pdf
    {
        public Pdf(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        public int PageCount { get; set; }

        public int ImageCount { get; set; }

        public static List<Pdf> List(params string[] paths) => new(paths.Select(x => new Pdf(x)));
    }
}
