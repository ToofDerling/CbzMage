namespace CbzMage.Shared.JobQueue
{
    public class JobEventArgs<T> : EventArgs
    {
        public JobEventArgs(T result)
        {
            Result = result;
        }

        public string Info { get; set; }

        public Exception Exception { get; set; }

        public T Result { get; private set; }
    }
}
