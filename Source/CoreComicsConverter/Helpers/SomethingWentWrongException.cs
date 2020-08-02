using System;

namespace CoreComicsConverter.Helpers
{
    public class SomethingWentWrongException : ApplicationException
    {
        public SomethingWentWrongException(string message) : base(message)
        {
        }
    }
}
