using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PubnubChatApi
{
    public class ExponentialRateLimiter : IDisposable
    {
        private const int ThreadsMaxSleepMs = 1000;
        private const int NoSleepRequired = -1;
        
        private readonly float _exponentialFactor;
        private readonly ConcurrentDictionary<string, RateLimiterRoot> _limiters;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _processorTask;
        private readonly object _processorLock = new object();
        private bool _disposed = false;

        public ExponentialRateLimiter(float exponentialFactor)
        {
            _exponentialFactor = exponentialFactor;
            _limiters = new ConcurrentDictionary<string, RateLimiterRoot>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async void RunWithinLimits(string id, int baseIntervalMs, Func<Task<object>> task, Action<object> callback, Action<Exception> errorCallback)
        {
            if (baseIntervalMs == 0)
            {
                // Execute immediately for zero interval
                try
                {
                    var result = await task().ConfigureAwait(false);
                    callback(result);
                }
                catch (Exception e)
                {
                    errorCallback(e);
                }
                return;
            }

            var limiter = _limiters.GetOrAdd(id, _ => new RateLimiterRoot
            {
                Queue = new Queue<TaskElement>(),
                CurrentPenalty = 0,
                BaseIntervalMs = baseIntervalMs,
                NextIntervalMs = 0,
                ElapsedMs = 0,
                Finished = false,
                LastTaskStartTime = DateTimeOffset.UtcNow
            });

            lock (limiter.Queue)
            {
                limiter.Queue.Enqueue(new TaskElement
                {
                    Task = task,
                    Callback = callback,
                    ErrorCallback = errorCallback,
                    Penalty = limiter.CurrentPenalty
                });
            }

            EnsureProcessorRunning();
        }

        private void EnsureProcessorRunning()
        {
            lock (_processorLock)
            {
                if (_processorTask == null || _processorTask.IsCompleted)
                {
                    _processorTask = Task.Run(ProcessorLoop, _cancellationTokenSource.Token);
                }
            }
        }

        private async Task ProcessorLoop()
        {
            var slept = 0;
            var toSleep = 0;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (toSleep == NoSleepRequired)
                {
                    break;
                }

                if (slept >= toSleep)
                {
                    toSleep = await ProcessQueue(slept).ConfigureAwait(false);
                }
                else
                {
                    toSleep -= slept;
                }

                slept = Math.Min(toSleep, ThreadsMaxSleepMs);
                
                if (slept > 0)
                {
                    await Task.Delay(slept, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        private async Task<int> ProcessQueue(int sleptMs)
        {
            var toSleep = ThreadsMaxSleepMs;
            var itemsToRemove = new List<string>();
            var processingTasks = new List<Task>();

            foreach (var kvp in _limiters)
            {
                var id = kvp.Key;
                var limiter = kvp.Value;

                lock (limiter.Queue)
                {
                    limiter.ElapsedMs += sleptMs;

                    if (limiter.NextIntervalMs > limiter.ElapsedMs)
                    {
                        toSleep = Math.Min(toSleep, limiter.NextIntervalMs - limiter.ElapsedMs);
                        continue;
                    }

                    // Start processing the task asynchronously
                    var processingTask = ProcessLimiterAsync(limiter);
                    processingTasks.Add(processingTask);

                    limiter.CurrentPenalty++;
                    limiter.NextIntervalMs = (int)(limiter.BaseIntervalMs * Math.Pow(_exponentialFactor, limiter.CurrentPenalty));
                    limiter.ElapsedMs = 0;
                    limiter.LastTaskStartTime = DateTimeOffset.UtcNow;

                    toSleep = Math.Min(toSleep, limiter.NextIntervalMs);

                    if (limiter.Finished)
                    {
                        itemsToRemove.Add(id);
                    }
                }
            }

            // Wait for all processing tasks to complete before continuing
            // This ensures we don't overwhelm the system with concurrent tasks
            if (processingTasks.Count > 0)
            {
                await Task.WhenAll(processingTasks).ConfigureAwait(false);
            }

            // Remove finished limiters
            foreach (var id in itemsToRemove)
            {
                _limiters.TryRemove(id, out _);
            }

            if (_limiters.IsEmpty)
            {
                return NoSleepRequired;
            }

            return toSleep;
        }

        private async Task ProcessLimiterAsync(RateLimiterRoot limiterRoot)
        {
            TaskElement element;
            
            // Queue is already locked by caller, but we need to dequeue safely
            lock (limiterRoot.Queue)
            {
                if (limiterRoot.Queue.Count == 0)
                {
                    limiterRoot.Finished = true;
                    return;
                }

                element = limiterRoot.Queue.Dequeue();
            }

            try
            {
                var result = await element.Task().ConfigureAwait(false);
                element.Callback(result);
            }
            catch (Exception e)
            {
                element.ErrorCallback(e);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cancellationTokenSource?.Cancel();
                
                try
                {
                    _processorTask?.Wait(5000); // Wait up to 5 seconds for graceful shutdown
                }
                catch (AggregateException)
                {
                    // Ignore cancellation exceptions during shutdown
                }

                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }

        private class RateLimiterRoot
        {
            public Queue<TaskElement> Queue { get; set; }
            public int CurrentPenalty { get; set; }
            public int BaseIntervalMs { get; set; }
            public int NextIntervalMs { get; set; }
            public int ElapsedMs { get; set; }
            public bool Finished { get; set; }
            public DateTimeOffset LastTaskStartTime { get; set; }
        }

        private class TaskElement
        {
            public Func<Task<object>> Task { get; set; }
            public Action<object> Callback { get; set; }
            public Action<Exception> ErrorCallback { get; set; }
            public int Penalty { get; set; }
        }
    }
}