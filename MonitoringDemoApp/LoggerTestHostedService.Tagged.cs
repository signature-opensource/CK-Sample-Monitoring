#define DEBUG
// This file is compiled in Debug mode in order to test the Debug.Assert/Fail and Trace.Fail behavior with the
// 

using CK.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringDemoApp
{
    public partial class LoggerTestHostedService : IHostedService, IDisposable
    {
        // Three tags: 2^3 combinations (including the AllDemoTags with the 3 and the empty tags).
        static readonly CKTrait Sql = ActivityMonitor.Tags.Register( "Sql" );
        static readonly CKTrait Machine = ActivityMonitor.Tags.Register( "Machine" );
        static readonly CKTrait Facebook = ActivityMonitor.Tags.Register( "Facebook" );

        // Tags can be combined with | (or +), intersected with &, excluded with - (except) or ^ (SymmetricExcept - an xor).
        static readonly CKTrait AllDemoTags = Sql|Machine|Facebook;

        int _level;
        bool _group;

        void OnTimeTaggedLogs( int workCount )
        {
            // Fallbacks is a capability of CKTrait that enumerates all the different combinations except the tag itself.
            // Beware: it's 2^(number of traits)-1. This is not efficient but simple.
            var letsTag = workCount % 8 == 0
                            ? AllDemoTags
                            : AllDemoTags.Fallbacks.ElementAt( (workCount % 8)-1 );
            if( letsTag == AllDemoTags )
            {
                Debug.Assert( 1 == (int)LogLevel.Debug );
                Debug.Assert( (1 << 5) == (int)LogLevel.Fatal );
                if( ++_level == 6 )
                {
                    _level = 1;
                    _group = !_group;
                }
            }

            if( _group )
            {
                _monitor.OpenGroup( (LogLevel)(1 << _level), letsTag, $"Tag n°{workCount}." )
                        .Dispose();
            }
            else
            {
                _monitor.Log( (LogLevel)(1 << _level), letsTag, $"Tag n°{workCount}." );
            }
        }
    }
}
