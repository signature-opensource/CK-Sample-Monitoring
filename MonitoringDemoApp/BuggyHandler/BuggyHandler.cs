using CK.Core;
using CK.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDemoApp;

class BuggyHandler : IGrandOutputHandler
{
    BuggyHandlerConfiguration.WhenToBug _when;

    public BuggyHandler( BuggyHandlerConfiguration configuration )
    {
        if( configuration.Bug == BuggyHandlerConfiguration.WhenToBug.Ctor )
        {
            throw new Exception( "Ctor bug." );
        }
        _when = configuration.Bug;
    }

    public ValueTask<bool> ActivateAsync( IActivityMonitor m )
    {
        if( _when == BuggyHandlerConfiguration.WhenToBug.Activate )
        {
            throw new Exception( $"Activation bug." );
        }
        return ValueTask.FromResult( true );
    }

    public ValueTask<bool> ApplyConfigurationAsync( IActivityMonitor m, IHandlerConfiguration c )
    {
        if( c is BuggyHandlerConfiguration configuration )
        {
            if( configuration.Bug == BuggyHandlerConfiguration.WhenToBug.ApplyConfiguration )
            {
                throw new Exception( $"Applying configuration bug." );
            }
            _when = configuration.Bug;
            return ValueTask.FromResult( true );
        }
        return ValueTask.FromResult( false );
    }

    public ValueTask DeactivateAsync( IActivityMonitor m )
    {
        if( _when == BuggyHandlerConfiguration.WhenToBug.Deactivate )
        {
            throw new Exception( $"Deactivation bug." );
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleAsync( IActivityMonitor m, InputLogEntry logEvent )
    {
        if( _when == BuggyHandlerConfiguration.WhenToBug.Handle )
        {
            throw new Exception( $"Handle log event bug." );
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask OnTimerAsync( IActivityMonitor m, TimeSpan timerSpan )
    {
        if( _when == BuggyHandlerConfiguration.WhenToBug.OnTimer )
        {
            throw new Exception( $"OnTimer bug." );
        }
        return ValueTask.CompletedTask;
    }
}
