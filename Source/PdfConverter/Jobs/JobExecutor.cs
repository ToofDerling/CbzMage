using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PdfConverter.Jobs
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

        private Thread _jobExecutorThread;

        private ThreadPriority _threadPriority;

        private InternalJobWaiter _jobWaiter;

        public JobExecutor(ThreadPriority priority = ThreadPriority.Normal)
        { 
            _threadPriority = priority;
        }

        public JobWaiter Start(bool withWaiter)
        {
            _runningQueue = new BlockingCollection<IJob<T>>();

            if (withWaiter)
            {
                _jobWaiter = new InternalJobWaiter();
            }

            _jobExecutorThread = new Thread(new ThreadStart(JobExecutorLoop))
            {
                Priority = _threadPriority
            };
            _jobExecutorThread.Start();

            return _jobWaiter;
        }

        public void Stop()
        {
            AddJob(null);
        }

        public void AddJob(IJob<T> job)
        {
            _runningQueue.Add(job);
        }

        private void JobExecutorLoop()
        {
            IJob<T> job;
            var jobCount = 0;

            while ((job = _runningQueue.Take()) != null)
            {
                var result = job.Execute();

                jobCount++;

                JobExecuted?.Invoke(this, new JobEventArgs<T>(result));
            }

            _jobWaiter?.SignalWaitIsOver();

            _runningQueue.Dispose();
        }

        public event EventHandler<JobEventArgs<T>> JobExecuted;
    }
}
