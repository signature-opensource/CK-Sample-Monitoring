using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonitoringDemoApp
{
    class Program
    {
        public static void Main( string[] args )
        {
            // This doesn't use the CreateDefaultBuilder helper.
            // Instead, the configuration here is detailed and explicit.
            var host = new HostBuilder()
                // Current directory is... complicated! See https://github.com/dotnet/project-system/issues/5053.
                // To overcome this mess, we set a MSBuild property <RunWorkingDirectory>$(MSBuildProjectDirectory)</RunWorkingDirectory>
                // so that the current directory here is the project root (just like Web applications).
                .UseContentRoot( Directory.GetCurrentDirectory() )
                // Any environment variables that start with "DOTNET_" are for the host.
                // This is why the launchSettings.json defines the "DOTNET_ENVIRONMENT" variables.
                .ConfigureHostConfiguration( config =>
                {
                    config.AddEnvironmentVariables( prefix: "DOTNET_" );
                    config.AddCommandLine( args );
                } )
                .ConfigureAppConfiguration( ( hostingContext, config ) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    // First configuration layer is the appsettings.json, followed by the one with the
                    // environment name "" (see the static class Microsoft.Extensions.Hosting.Environments).
                    config.AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
                          .AddJsonFile( $"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true );

                    // Configuration coming from the environment variables are considered safer than appsettings configuration.
                    // We add them after the appsettings.
                    config.AddEnvironmentVariables();

                    // Then, in development mode, can come the User Secrets.
                    // To activate it, adds a new UserSecretsId GUID Property to this csproj.
                    //
                    //     <PropertyGroup>
                    //          <UserSecretsId>{generates a new guid or use a shared one between different projects}</UserSecretsId>
                    //     </PropertyGroup>
                    // 
                    //  if( env.IsDevelopment() )
                    //  {
                    //    config.AddUserSecrets<Program>( optional: true );
                    //  }
                    //

                    // Finally comes the configuration values from the command line: these configurations override
                    // any previous ones.
                    config.AddCommandLine( args );
                } )
                .ConfigureLogging( ( hostingContext, logging ) =>
                {
                    // Standard .Net logger configuration.
                    // By default, we route all the .Net logs to a special "monitor" that is the "§ext": this
                    // special (fake) monitor captures all contextless logs sent to GrandOutput.ExternalLog methods.

                    // This is added *before* configuration is loaded: configuration can override this default.
                    // By default, we log everything in Development mode and only Information level otherwise.
                    if( hostingContext.HostingEnvironment.IsDevelopment() )
                    {
                        logging.AddFilter( level => true );
                    }
                    else
                    {
                        logging.AddFilter( level => level >= LogLevel.Information );
                    }
                    // Logging is the default section name to use for .Net logging.
                    logging.AddConfiguration( hostingContext.Configuration.GetSection( "Logging" ) );
                } )
                // This configures the GrandOutput.Default and provides a scoped IActivityMonitor to the DI.
                // The section name MUST BE "CK-Monitoring".
                .UseCKMonitoring()
                .ConfigureServices( (host, services) =>
                {
                    // Registers the hosted service that periodically sends logs (and its configuration).
                    services.Configure<LoggerTestHostedServiceConfiguration>( host.Configuration.GetSection( "StupidLogGenerator" ) );
                    services.AddHostedService<LoggerTestHostedService>();
                } )
                .Build();

            host.Run();
        }


    }
}
