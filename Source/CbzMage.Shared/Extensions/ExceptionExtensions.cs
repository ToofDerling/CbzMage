namespace CbzMage.Shared.Extensions
{
    public static class ExceptionExtensions
    {
        public static string TypeAndMessage(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}";
    }
}
