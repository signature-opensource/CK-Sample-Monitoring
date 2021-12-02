using CK.Core;
using CK.Monitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDemoApp
{
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

        public bool Activate( IActivityMonitor m )
        {
            if( _when == BuggyHandlerConfiguration.WhenToBug.Activate )
            {
                throw new Exception( $"Activation bug." );
            }
            return true;
        }

        public bool ApplyConfiguration( IActivityMonitor m, IHandlerConfiguration c )
        {
            if( c is BuggyHandlerConfiguration configuration )
            {
                if( configuration.Bug == BuggyHandlerConfiguration.WhenToBug.ApplyConfiguration )
                {
                    throw new Exception( $"Applying configuration bug." );
                }
                _when = configuration.Bug;
                return true;
            }
            return false;
        }

        public void Deactivate( IActivityMonitor m )
        {
            if( _when == BuggyHandlerConfiguration.WhenToBug.Deactivate )
            {
                throw new Exception( $"Deactivation bug." );
            }
        }

        public void Handle( IActivityMonitor m, GrandOutputEventInfo logEvent )
        {
            if( _when == BuggyHandlerConfiguration.WhenToBug.Handle )
            {
                throw new Exception( $"Handle log event bug." );
            }
        }

        public void OnTimer( IActivityMonitor m, TimeSpan timerSpan )
        {
            if( _when == BuggyHandlerConfiguration.WhenToBug.OnTimer )
            {
                throw new Exception( $"OnTimer bug." );
            }
        }
    }
}
