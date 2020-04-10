namespace TimerExample
{
    using System;
    using System.Threading.Tasks;
    using Autocrat.Abstractions;

    internal sealed class TimerHandler
    {
        private readonly IWorkerFactory workerFactory;

        public TimerHandler(IWorkerFactory workerFactory)
        {
            this.workerFactory = workerFactory;
        }

        public async Task OnTimerAsync(int token)
        {
            TimeSpan now = DateTime.UtcNow.TimeOfDay;

            // Temporary workaround until the GC is handled by the native code
            GC.TryStartNoGCRegion(1024 * 1024);

            TimerState state = await this.workerFactory.GetWorkerAsync<TimerState>(token);
            state.InvocationCount++;

            Console.WriteLine("{0}: {1}", now, state.InvocationCount);

            GC.EndNoGCRegion();
        }

        public class TimerState
        {
            public TimerState()
            {
                Console.WriteLine("TimerState constructor");
            }

            public int InvocationCount { get; set; }
        }
    }
}
