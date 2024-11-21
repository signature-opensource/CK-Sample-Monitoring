using CK.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDemoApp;

class BuggyHandlerConfiguration : IHandlerConfiguration
{
    public enum WhenToBug
    {
        Never,
        Ctor,
        Activate,
        ApplyConfiguration,
        Deactivate,
        Handle,
        OnTimer
    }

    /// <summary>
    /// Defines when the handler will bug.
    /// Activation bugs once every 10 activations (this cannot be configured since Activate has no configuration yet).
    /// </summary>
    public WhenToBug Bug { get; set; }

    public IHandlerConfiguration Clone()
    {
        return new BuggyHandlerConfiguration
        {
            Bug = Bug
        };
    }
}
