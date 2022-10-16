using System;

namespace PdfConverter.Jobs
{
    public class JobEventArgs<T> : EventArgs
    {
        public JobEventArgs(T result)
        {
            Result = result;
        }

        public T Result { get; private set; }
    }
}
