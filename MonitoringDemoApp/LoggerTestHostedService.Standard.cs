#define DEBUG
// This file is compiled in Debug mode in order to test the Debug.Assert/Fail and Trace.Fail behavior with the
// 

using CK.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringDemoApp
{
    public partial class LoggerTestHostedService : IHostedService, IDisposable
    {
        void OnTimeStandardLogs( int workCount )
        {
            switch( workCount % 16 )
            {
                case 0: _monitor.Debug( "A debug line (most verbose level)." ); break;
                case 1: _monitor.Trace( $"A trace line (not always the same: {Guid.NewGuid()})." ); break;
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
                        if( _options.CurrentValue.Mode == LoggerTestHostedServiceConfiguration.WorkingMode.StandardWithUnobservedTaskException )
                        {
                            _monitor.Trace( @"Calling: Task.Run( () => throw new Exception( ""Unhandled exception on the default Task scheduler."" ) );

This 'lost' exception will be hooked by a TaskScheduler.UnobservedTaskException an logged... but at the next GC time!
" );
                            _ = Task.Run( () => throw new Exception( "Unhandled exception on the default Task scheduler." ) );
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
                case 14:
                    {
                        CheckSafeFailFast( () =>
                        {
                            Debug.Assert( false, "Debug.Assert failure results in a CK.Monitoring.MonitoringFailFastException and not a fail fast of the application." );
                        } );

                        CheckSafeFailFast( () =>
                        {
                            Trace.Assert( false, "Trace.Assert failure results in a CK.Monitoring.MonitoringFailFastException and not a fail fast of the application." );
                        } );

                        void CheckSafeFailFast( Action a )
                        {
                            try
                            {
                                a();
                            }
                            catch( CK.Monitoring.MonitoringFailFastException ex )
                            {
                                _monitor.Info( $"Received a MonitoringFailFastException with message: {ex.Message}" );
                            }
                        }

                        break;
                    }
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
    }
}
