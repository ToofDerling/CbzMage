using ImageMagick;

namespace PdfConverter.Model
{
    public class ComicPage
    {
        public string Name { get; set; }

        public int Number { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int NewWidth { get; set; }

        public int NewHeight { get; set; }

        public string Path { get; set; }

        public void Ping()
        {
            using var image = new MagickImage();
            image.Ping(Path);

            Width = image.Width;
            Height = image.Height;
        }
    }
}
