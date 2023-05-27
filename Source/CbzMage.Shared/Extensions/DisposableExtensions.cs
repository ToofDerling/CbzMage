namespace CbzMage.Shared.Extensions
{
    public static class DisposableExtensions
    {
        public static void DisposeDontCare(this IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch
            {
            }
        }
    }
}

