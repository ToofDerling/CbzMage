namespace CbzMage.Shared.JobQueue
{
    public class JobProducerConsumer<T> : AbstractJobQueue<T>
    {
        public JobProducerConsumer(int numWorkerThreads = 1) : base(numWorkerThreads)
        {
        }

        public JobWaiter Start(IJobProducer<T> producer, bool withWaiter)
        {
            var jobWaiter = InitQueueWaiterAndWorkerThreads(withWaiter);

            Task.Factory.StartNew(() => producer.ProduceAsync(_jobQueue), TaskCreationOptions.LongRunning);

            return jobWaiter;
        }
    }
}
