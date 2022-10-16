using System;

namespace PdfConverter.Extensions
{
    public static class ExceptionExtensions
    {
        public static string TypeAndMessage(this Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
