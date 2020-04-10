namespace TimerExample
{
    using System;
    using Autocrat.Abstractions;

    public class Initializer : IInitializer
    {
        private readonly ITimerService timerService;

        public Initializer(ITimerService timerService)
        {
            this.timerService = timerService;
        }

        public void OnConfigurationLoaded()
        {
            var handler = new TimerHandler(null);
            this.timerService.RegisterRepeat(TimeSpan.FromSeconds(1), handler.OnTimerAsync);
            Console.WriteLine("Registered timer");
        }
    }
}
