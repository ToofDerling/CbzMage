namespace CbzMage.Shared.Extensions
{
    public static class PageStringExtensions
    {
        public static string ToPageString(this int pageNumber)
        {
            return ToPageString(pageNumber, "jpg");
        }

        public static string ToPageString(this int pageNumber, string imageExt)
        {
            var page = pageNumber.ToString().PadLeft(4, '0');
            return $"page-{page}.{imageExt}";
        }

        public static int ToPageNumber(this string page)
        {
            var idx = page.IndexOf('-');
            var number = page.Substring(idx + 1, 4);

            return int.Parse(number);
        }
    }
}
