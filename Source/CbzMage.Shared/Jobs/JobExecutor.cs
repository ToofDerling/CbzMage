namespace CbzMage.Shared.Jobs
{
    public class JobExecutor<T> : AbstractJobQueue<T>
    {
        public JobExecutor(int numWorkerThreads = 1) : base(numWorkerThreads) 
        {
        }

        public JobWaiter Start(bool withWaiter)
        { 
            return InitQueueWaiterAndWorkerThreads(withWaiter);
        }
    }
}
