# Monitoring samples

This repository contains samples of ActivityMonitor and GrandOutput use and configuration.

Code and configuration files in this repository are heavily documented: this aims to
be an always-synchronized documentation and how-to for anything related to the CK stack
monitoring tools.

A sample for CK.Testing.Monitoring is currently missing.

## ColoredConsoleDemo

This is a very basic example of logging with the Console client.

## MonitoringDemoApp

This is a complete console application configured by its [appsettings.json](MonitoringDemoApp/appsettings.json) and
also a sample of a typical `IHostedService` (the [LoggerTestHostedService](MonitoringDemoApp/LoggerTestHostedService.cs)) with
its own (also dynamically monitored) configuration [LoggerTestHostedServiceConfiguration](MonitoringDemoApp/LoggerTestHostedServiceConfiguration.cs).

Changing the configuration file (with any file editor) is immediately reflected in the logs that the hosted service
emits.

