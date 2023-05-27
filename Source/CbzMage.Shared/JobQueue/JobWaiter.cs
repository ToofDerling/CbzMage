using System.Collections.Concurrent;

namespace CbzMage.Shared.JobQueue
{
    public abstract class JobWaiter
    {
        protected readonly BlockingCollection<string> _waitingQueue;

        protected JobWaiter()
        {
            _waitingQueue = new BlockingCollection<string>();
        }

        public void WaitForJobsToFinish()
        {
            try
            {
                _waitingQueue.Take();
            }
            finally
            {
                _waitingQueue.Dispose();
            }
        }
    }
}
