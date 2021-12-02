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

        // Tags can be combined with |, intersected with &, excluded with - (except) or ^ (SymmetricExcept).
        static readonly CKTrait AllDemoTags = Sql|Machine|Facebook;

        void OnTimeTaggedLogs()
        {
            // Fallbacks is a capability of CKTrait that enumerates all the different combinations except the tag itself.
            // Beware: it's 2^(number of traits)-1. This is not efficient but simple.
            var letsTag = _workCount % 8 == 0
                            ? AllDemoTags
                            : AllDemoTags.Fallbacks.ElementAt( (_workCount % 8)-1 );
            var level = _workCount % 6;
            var isGroup = _workCount % 2 == 0;

            if( isGroup )
            {
                _monitor.OpenGroup( (LogLevel)(1 << level), letsTag, $"Tag n°{_workCount}" ).Dispose();
            }
            else
            {
                _monitor.Log( (LogLevel)(1 << level), letsTag, $"Tag n°{_workCount}" );
            }
        }
    }
}
