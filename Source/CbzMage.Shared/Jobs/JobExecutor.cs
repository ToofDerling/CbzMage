using System.Collections.Concurrent;

namespace CbzMage.Shared.Jobs
{
    public class JobExecutor<T>
    {
        private class InternalJobWaiter : JobWaiter
        {
            public void SignalWaitIsOver()
            {
                _waitingQueue.Add("Stop");
            }
        }

        private BlockingCollection<IJob<T>> _runningQueue;

        private InternalJobWaiter _jobWaiter;

        private readonly int _numThreads;

        private int _numFinishedThreads = 0;

        public JobExecutor(int numThreads = 1)
        {
            _numThreads = numThreads;
        }

        public JobWaiter Start(bool withWaiter)
        {
            _runningQueue = new BlockingCollection<IJob<T>>();

            if (withWaiter)
            {
                _jobWaiter = new InternalJobWaiter();
            }

            for (int i = 0; i < _numThreads; i++)
            {
                Task.Factory.StartNew(JobExecutorLoopAsync, TaskCreationOptions.LongRunning);
            }

            return _jobWaiter;
        }

        public void Stop()
        {
            _runningQueue.CompleteAdding();
        }

        public void AddJob(IJob<T> job)
        {
            _runningQueue.Add(job);
        }

        private async Task JobExecutorLoopAsync()
        {
            foreach (var job in _runningQueue.GetConsumingEnumerable())
            {
                var result = await job.ExecuteAsync();

                JobExecuted?.Invoke(this, new JobEventArgs<T>(result));
            }

            if (Interlocked.Increment(ref _numFinishedThreads) == _numThreads)
            {
                _jobWaiter?.SignalWaitIsOver();

                _runningQueue.Dispose();
            }
        }

        public event EventHandler<JobEventArgs<T>> JobExecuted;
    }
}
