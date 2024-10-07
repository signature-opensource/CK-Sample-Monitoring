using CK.Core;
using System.Diagnostics;

namespace ColoredConsoleDemo;

class Program
{
    static void Main( string[] args )
    {
        var m = new ActivityMonitor( topic: "I'm full of colors (and I'm playing with levels and filters)." );

        m.Output.RegisterClient( new ColoredActivityMonitorConsoleClient() );

        m.MinimalFilter = LogFilter.Terse;
        using( m.OpenInfo( $"Let's go: the actual filter is {m.ActualFilter}." ) )
        {
            m.Info( "In Terse filter, only Info (and above) groups and only Error or Fatal lines are logged. So you won't see this." );

            using( m.OpenError( "A whole group of error." ) )
            {
                m.Debug( "Debug is the lowest level." );
                m.Debug( $"In an Error or Fatal group, the minimal filter is automatically set to {m.ActualFilter} (that's why you can see this line)." );
                m.Trace( "Trace is low level." );
                m.Info( "Info is interesting." );
                m.Warn( "Warning is not serious." );
                m.Error( "Error should be investigated." );
                m.Fatal( "Fatal ruins the process." );

                m.CloseGroup( "This concludes the error (the using } close below will do nothing)." );
            }
            m.MinimalFilter = LogFilter.Verbose;
            m.Info( "Verbose filter let's the Info lines be logged." );
            m.Info( "Note that closing a group restores the minimal filter level that was set on the opening." );
        }
        Debug.Assert( m.ActualFilter == LogFilter.Terse );
        m.MinimalFilter = LogFilter.Monitor;
        m.Warn( "Using Monitor filter, only Warn, Error and Fatal lines are logged." );

        System.Console.ReadLine();
    }
}
