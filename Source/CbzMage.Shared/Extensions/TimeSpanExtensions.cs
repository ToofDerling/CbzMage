namespace CbzMage.Shared.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string Mmss(this TimeSpan a) => a.ToString(@"mm\:ss");

        public static string Hhmmss(this TimeSpan a) => a.ToString(@"hh\:mm\:ss");
    }
}
