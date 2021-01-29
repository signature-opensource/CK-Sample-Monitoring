using CK.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringDemoApp
{

    /// <summary>
    /// This background service emits logs on a regular basis controlled by <see cref="LoggerTestHostedServiceConfiguration"/>
    /// that can be dynamically updated.
    /// </summary>
    public class LoggerTestHostedService : IHostedService, IDisposable
    {
        readonly ILoggerFactory _dotNetLoggerFactory;
        readonly IOptionsMonitor<LoggerTestHostedServiceConfiguration> _options;
        readonly IActivityMonitor _monitor;
        Timer _timer;
        ILogger _dotNetLogger;

        int _reentrancyFlag;
        int _workCount = 0;
        bool _dirtyOption;

        public LoggerTestHostedService( ILoggerFactory dotNetLoggerFactory, IOptionsMonitor<LoggerTestHostedServiceConfiguration> options )
        {
            _dotNetLoggerFactory = dotNetLoggerFactory;
            _options = options;
            _monitor = new ActivityMonitor( $"I'm monitoring '{nameof(LoggerTestHostedService)}'." );
            _monitor.Info( $"Initially: {options.CurrentValue}" );
            _options.OnChange( ( config, name ) =>
            {
                // Monitoring the change here would be a mistake: the OnChange is called from a background thread
                // that can be in parallel with OnTime call (and even StartAsync: we may not even have a timer here).
                // The safest and cleanest way to handle this is simply to set a dirty flag that will trigger the timer reconfiguration
                // in the next OnTime... or right now (since OnTime reentrancy is safe).
                _dirtyOption = true;
                OnTime( null );
            } );
        }

        public Task StartAsync( CancellationToken stoppingToken )
        {
            _monitor.Info( "LoggerTestHostedService started." );
            _timer = new Timer( OnTime, null, TimeSpan.Zero, _options.CurrentValue.TimerDuration );
            return Task.CompletedTask;
        }

        void OnTime( object state )
        {
            // This is a simple lock that is non reentrant.
            // One may perfectly have used a standard .Net lock object here (Monitor.Enter/Exit) but
            // this is even lighter than standard lock.
            if( Interlocked.CompareExchange( ref _reentrancyFlag, 1, 0 ) == 0 )
            {
                HandleDirtyOption();
                using( _monitor.OpenInfo( $"Work n°{++_workCount}." ) )
                {
                    switch( _workCount % 15 )
                    {
                        case 0: _monitor.Debug( "A debug line (most verbose level)." ); break;
                        case 1: _monitor.Trace( "A trace line." ); break;
                        case 2: _monitor.Info( "An info line." ); break;
                        case 3: _monitor.Warn( "A warning line." ); break;
                        case 4: _monitor.Error( "An error line." ); break;
                        case 5: _monitor.Fatal( "A fatal line (most severe level)." ); break;
                        case 6:
                            {
                                _monitor.Info( @"This would crash the entire process: throw new Exception( ""Unhandled exception, directly on the thread pool, during the timer handling."" ); " );
                                _monitor.Debug( @"
My first idea was that such an exception directly on the timer thread would have been trapped by AppDomain.CurrentDomain.UnhandledException. This is not the case:
see the (closed) issue: https://github.com/dotnet/extensions/issues/1836

""
The application exits because entire process is crashing. The host isn't gracefully shutting down, the process is just dying.
This happens when using timers or the thread pool directly without a try catch.
""
" );
                                break;
                            }
                        case 7:
                            {
                                if( _options.CurrentValue.ThrowTaskSchedulerUnobservedTaskException )
                                {
                                    _monitor.Trace( @"Calling: Task.Run( () => throw new Exception( ""Unhandled exception on the default Task scheduler."" ) );

This 'lost' exception will be hooked by a TaskScheduler.UnobservedTaskException an logged... but at the next GC time!
" );
                                    Task.Run( () => throw new Exception( "Unhandled exception on the default Task scheduler." ) );
                                }
                                else _monitor.Trace( @"Throwing unhandled exception has been skipped since ThrowTaskSchedulerUnobservedTaskException is false." );
                                break;
                            }
                        case 8:
                            {
                                if( _dotNetLogger == null )
                                {
                                    _monitor.Info( "Creating the '.Net Standard Demo' logger (this case n°8 emits a .Net LogTrace below)." );
                                    _dotNetLogger = _dotNetLoggerFactory.CreateLogger( ".Net Standard Demo" );
                                }
                                _dotNetLogger.LogTrace( $".Net LogTrace (most verbose .Net log level)." );
                                break;
                            }
                        case 9: _dotNetLogger.LogDebug( $".Net LogDebug (Debug is less verbose than Trace fo .Net logs)." ); break;
                        case 10: _dotNetLogger.LogInformation( $".Net LogInformation." ); break;
                        case 11: _dotNetLogger.LogWarning( $".Net LogWarning." ); break;
                        case 12: _dotNetLogger.LogError( $".Net LogError." ); break;
                        case 13: _dotNetLogger.LogCritical( $".Net LogCritical (most severe .Net level)." ); break;
                        default:
                            using( _monitor.OpenInfo( "Calling the Garbage collector: this will release the TaskScheduler.UnobservedTaskException." ) )
                            {
                                GC.Collect();
                                _monitor.Trace( "Waiting for completion." );
                                GC.WaitForFullGCComplete();
                            }
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
                _timer.Change( TimeSpan.Zero, _options.CurrentValue.TimerDuration );
            }
        }

        public Task StopAsync( CancellationToken stoppingToken )
        {
            _monitor.Info( "LoggerTestHostedService stopped." );
            _timer.Change( Timeout.Infinite, 0 );
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
