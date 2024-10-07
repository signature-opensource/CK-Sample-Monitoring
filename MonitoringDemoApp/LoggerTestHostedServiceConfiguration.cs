using System;
using System.Threading.Tasks;

namespace MonitoringDemoApp;

/// <summary>
/// Configures the <see cref="LoggerTestHostedService"/>.
/// </summary>
public class LoggerTestHostedServiceConfiguration
{
    public enum WorkingMode
    {
        None,
        Standard,
        StandardWithUnobservedTaskException,
        Tagged
    }

    /// <summary>
    /// Gets or sets the timer duration.
    /// </summary>
    public TimeSpan TimerDuration { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WorkingMode"/>.
    /// </summary>
    public WorkingMode Mode { get; set; } = WorkingMode.Standard;

    public override string ToString() => $"Options{{ TimerDuration = {TimerDuration}, Mode = {Mode} }}";
}
