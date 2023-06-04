namespace PdfConverter
{
    /* itext code:
         return IdentifyImageType() switch
         {
             ImageType.PNG => "png",
             ImageType.JPEG => "jpg",
             ImageType.JPEG2000 => "jp2",
             ImageType.TIFF => "tif",
             ImageType.JBIG2 => "jbig2",
             _ => throw new InvalidOperationException("Should have never happened. This type of image is not allowed for ImageXObject"),
         };
         */
    internal class ImageExt
    {
        public const string Jpg = "jpg";

        public const string Png = "png";
    }
}
