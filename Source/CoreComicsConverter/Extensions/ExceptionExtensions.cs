using System;

namespace Rotvel.PdfConverter.Extensions
{
    public static class ExceptionExtensions
    {
        public static string TypeAndMessage(this Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
