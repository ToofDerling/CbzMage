namespace EpubConverter
{
    public sealed class Epub
    {
        public Epub(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        public List<string> PageList { get; set; }
        
        public List<string> ImageList { get; set; }

        public string NavXhtml { get; set; }

        public static List<Epub> List(params string[] paths) => new(paths.Select(x => new Epub(x)));
    }
}
