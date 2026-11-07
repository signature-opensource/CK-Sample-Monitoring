using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonitoringDemoApp;

class Program
{
    public static void Main( string[] args )
    {
        // This doesn't use the defaults of Host.CreateApplicationBuilder.
        // Instead, the configuration here is detailed and explicit.

        // Any environment variables that start with "DOTNET_" are for the host.
        // This is why the launchSettings.json defines the "DOTNET_ENVIRONMENT" variables.
        var hostConfiguration = new ConfigurationManager();
        hostConfiguration.AddEnvironmentVariables( prefix: "DOTNET_" );
        hostConfiguration.AddCommandLine( args );

        var builder = Host.CreateApplicationBuilder( new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            Args = args,
            Configuration = hostConfiguration,
            // Current directory is... complicated! See https://github.com/dotnet/project-system/issues/5053.
            // To overcome this mess, we set a MSBuild property <RunWorkingDirectory>$(MSBuildProjectDirectory)</RunWorkingDirectory>
            // so that the current directory here is the project root (just like Web applications).
            ContentRootPath = Directory.GetCurrentDirectory()
        } );
        var env = builder.Environment;

        // First configuration layer is the appsettings.json, followed by the one with the
        // environment name "" (see the static class Microsoft.Extensions.Hosting.Environments).
        builder.Configuration.AddJsonFile( "appsettings.json", optional: true, reloadOnChange: true )
                             .AddJsonFile( $"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true );

        // Configuration coming from the environment variables are considered safer than appsettings configuration.
        // We add them after the appsettings.
        builder.Configuration.AddEnvironmentVariables();

        // Then, in development mode, can come the User Secrets.
        // To activate it, adds a new UserSecretsId GUID Property to this csproj.
        //
        //     <PropertyGroup>
        //          <UserSecretsId>{generates a new guid or use a shared one between different projects}</UserSecretsId>
        //     </PropertyGroup>
        //
        //  if( env.IsDevelopment() )
        //  {
        //    builder.Configuration.AddUserSecrets<Program>( optional: true );
        //  }
        //

        // Finally comes the configuration values from the command line: these configurations override
        // any previous ones.
        builder.Configuration.AddCommandLine( args );

        // Standard .Net logger configuration.
        // By default, we route all the .Net logs to a special "monitor" that is the "§ext": this
        // special (fake) monitor captures all contextless logs sent to GrandOutput.ExternalLog methods.

        // This is added *before* configuration is loaded: configuration can override this default.
        // By default, we log everything in Development mode and only Information level otherwise.
        if( env.IsDevelopment() )
        {
            builder.Logging.AddFilter( level => true );
        }
        else
        {
            builder.Logging.AddFilter( level => level >= LogLevel.Information );
        }
        // Logging is the default section name to use for .Net logging.
        builder.Logging.AddConfiguration( builder.Configuration.GetSection( "Logging" ) );

        // This configures the GrandOutput.Default from the "CK-Monitoring" section (the section name MUST BE "CK-Monitoring").
        // No IActivityMonitor is registered: this application creates its own monitors.
        builder.UseCKMonitoring();

        // Registers the hosted service that periodically sends logs (and its configuration).
        builder.Services.Configure<LoggerTestHostedServiceConfiguration>( builder.Configuration.GetSection( "StupidLogGenerator" ) );
        builder.Services.AddHostedService<LoggerTestHostedService>();

        var host = builder.CKBuild();
        host.Run();
    }
}
