using System;
using Integration.Common.Classes;
using Integration.Host.Features.AgentOutputFile;
using MetalHeaven.Agent.Shared.External.Classes;
using MetalHeaven.Agent.Shared.External.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Integration.Host;

public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
            {
                // register agent message serialization helper
                services.AddTransient<IAgentMessageSerializationHelper, ExternalAgentMessageSerializationHelper>();

                // register agent settings
                services.AddOptions<IntegrationSettings>().Bind(hostContext.Configuration.GetSection("IntegrationSettings"))
                    .ValidateDataAnnotations();

                // register agent output file watcher service
                services.AddHostedService<AgentOutputFileWatcherService>();

                // register MediatR with current assembly
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(AgentOutputFileWatcherService).Assembly);
                });
            });
}
