namespace Rotvel.PdfConverter.Extensions
{
    public static class StringExtensions
    {
        public static bool EqualsIgnoreCase(this string a, string b)
        {
            return string.Compare(a, b, true) == 0;
        }
    }
}
