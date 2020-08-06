namespace CoreComicsConverter.Dpi
{
    public static class ImageSizeExtensions
    {
        public static bool IsSmallerThan(this (int width, int height) thisImageSize, (int width, int height) thatImageSize)
        {
            return thisImageSize.GetDifference(thatImageSize) < 0;
        }

        public static bool IsLargerThan(this (int width, int height) thisImageSize, (int width, int height) thatImageSize)
        {
            return thisImageSize.GetDifference(thatImageSize) > 0;
        }

        public static int GetDifference(this (int width, int height) thisImageSize, (int width, int height) thatImageSize)
        {
            return (thisImageSize.width * thisImageSize.height) - (thatImageSize.width * thatImageSize.height);
        }
    }
}
