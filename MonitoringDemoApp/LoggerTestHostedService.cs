#define DEBUG
// This file is compiled in Debug mode in order to test the Debug.Assert/Fail and Trace.Fail behavior with the
// 

using CK.Core;
using CK.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringDemoApp;


/// <summary>
/// This background service emits logs on a regular basis controlled by <see cref="LoggerTestHostedServiceConfiguration"/>
/// that can be dynamically updated.
/// </summary>
public partial class LoggerTestHostedService : IHostedService, IDisposable
{
    readonly ILoggerFactory _dotNetLoggerFactory;
    readonly IOptionsMonitor<LoggerTestHostedServiceConfiguration> _options;
    readonly IHostEnvironment _hostEnvironment;
    readonly IActivityMonitor _monitor;
    Timer _timer;
    ILogger _dotNetLogger;

    int _reentrancyFlag;
    int _workCount = 0;
    bool _dirtyOption;

    public LoggerTestHostedService( ILoggerFactory dotNetLoggerFactory,
                                    IOptionsMonitor<LoggerTestHostedServiceConfiguration> options,
                                    IHostEnvironment hostEnvironment )
    {
        _dotNetLoggerFactory = dotNetLoggerFactory;
        _options = options;
        _hostEnvironment = hostEnvironment;
        _monitor = new ActivityMonitor( $"I'm monitoring '{nameof( LoggerTestHostedService )}'." );
        _monitor.Info( $"Initially: {options.CurrentValue}" );
        _options.OnChange( ( config, name ) =>
        {
            // Monitoring the change here would be a mistake: the OnChange is called from a background thread
            // that can be in parallel with OnTime call (and even StartAsync: we may not even have instantiated the timer here!).
            // The safest and cleanest way to handle this is simply to set a dirty flag that will trigger the timer reconfiguration
            // in the next OnTime... or right now (since OnTime reentrancy is safe).
            _dirtyOption = true;
            OnTime( null );
        } );
    }

    public Task StartAsync( CancellationToken stoppingToken )
    {
        // This monitor is tied to this service. It is solicited from (and only from)
        // the OnTime method that prevents reentrancy: only one thread enters the monitor at a time.
        _monitor.Info( "LoggerTestHostedService started." );
        _timer = new Timer( OnTime, null, TimeSpan.Zero, _options.CurrentValue.TimerDuration );
        return Task.CompletedTask;
    }

    void OnTime( object state )
    {
        // This is a simple lock that is non reentrant.
        // One may perfectly have used a standard .Net lock object here (Monitor.Enter/Exit) but
        // this is lighter than standard lock (since we don't want to wait to enter the lock, we just want to
        // skip the run).
        if( Interlocked.CompareExchange( ref _reentrancyFlag, 1, 0 ) == 0 )
        {
            HandleDirtyOption();
            ++_workCount;
            if( _workCount == 1 )
            {
                // Configuring the CoreApplicationIdentity after the start is possible.
                CoreApplicationIdentity.Configure( b =>
                {
                    b.DomainName = "SignatureSample";
                    b.EnvironmentName = _hostEnvironment.EnvironmentName;
                    b.PartyName = "MonitoringDemoApp";
                    b.ContextDescriptor = Environment.CommandLine;
                }, initialize: true );
            }
            using( _monitor.OpenInfo( $"Work n°{_workCount}." ) )
            {
                switch( _options.CurrentValue.Mode )
                {
                    case LoggerTestHostedServiceConfiguration.WorkingMode.StandardWithUnobservedTaskException:
                    case LoggerTestHostedServiceConfiguration.WorkingMode.Standard:
                        OnTimeStandardLogs( _workCount );
                        break;
                    case LoggerTestHostedServiceConfiguration.WorkingMode.Tagged:
                        OnTimeTaggedLogs( _workCount );
                        break;
                }
            }
            HandleDirtyOption();
            Interlocked.Exchange( ref _reentrancyFlag, 0 );
        }
    }

    void HandleDirtyOption()
    {
        if( _dirtyOption )
        {
            _dirtyOption = false;
            var t = _options.CurrentValue.TimerDuration;
            if( t < TimeSpan.FromMilliseconds( 1 ) )
            {
                _monitor.Error( "Invalid TimerDuration. Cannot be less than 00:00:00.001." );
            }
            else
            {
                _timer.Change( TimeSpan.Zero, t );
            }
        }
    }

    public Task StopAsync( CancellationToken stoppingToken )
    {
        // MonitorEnd is a way to tell the system: "I won't use this monitor anymore".
        // It tells tools that past this point, this monitor can safely be forgotten and no more
        // logs should be sent through it.
        // But this is just a hint: ending a monitor like this is absolutely not a requirement.
        _monitor.MonitorEnd( "LoggerTestHostedService stopped." );
        _timer.Change( Timeout.Infinite, 0 );
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
