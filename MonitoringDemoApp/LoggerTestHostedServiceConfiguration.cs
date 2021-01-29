using System;
using System.Threading.Tasks;

namespace MonitoringDemoApp
{
    /// <summary>
    /// Configures the <see cref="LoggerTestHostedService"/>.
    /// </summary>
    public class LoggerTestHostedServiceConfiguration
    {
        /// <summary>
        /// Gets or sets the timer duration.
        /// </summary>
        public TimeSpan TimerDuration { get; set; }

        /// <summary>
        /// Gets or sets whether <see cref="TaskScheduler.UnobservedTaskException"/> should be sollicited.
        /// </summary>
        public bool ThrowTaskSchedulerUnobservedTaskException { get; set; } = true;

        public override string ToString() => $"Options{{ TimerDuration = {TimerDuration} }}";
    }
}
