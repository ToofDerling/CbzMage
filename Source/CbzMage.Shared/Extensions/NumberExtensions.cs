namespace CbzMage.Shared.Extensions
{
    public static class NumberExtensions
    {
        public static int ToInt(this float f) => Convert.ToInt32(f);

        public static int ToInt(this double d) => Convert.ToInt32(d);
    }
}
