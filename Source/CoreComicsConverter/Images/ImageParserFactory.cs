using CoreComicsConverter.Model;

namespace CoreComicsConverter.Images
{
    public static class ImageParserFactory
    {
        public static ImageParser CreateFrom(Comic comic)
        {
            switch (comic.Type)
            {
                case ComicType.Pdf:
                    return new PdfImageParser(comic);
                case ComicType.Cbz:
                case ComicType.Directory:
                    return new DirectoryImageParser(comic);
                default:
                    return null;
            }
        }
    }
}
