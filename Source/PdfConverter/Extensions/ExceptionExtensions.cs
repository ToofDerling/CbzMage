using System;

namespace CoreComicsConverter.Extensions
{
    public static class ExceptionExtensions
    {
        public static string TypeAndMessage(this Exception ex) => $"{ex.GetType().Name}: {ex.Message}";
    }
}
