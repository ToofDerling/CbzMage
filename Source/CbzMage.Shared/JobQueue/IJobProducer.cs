using System.Collections.Concurrent;

namespace CbzMage.Shared.JobQueue
{
    public interface IJobProducer<T>
    {
        Task ProduceAsync(BlockingCollection<IJobConsumer<T>> jobQueue);
    }
}
